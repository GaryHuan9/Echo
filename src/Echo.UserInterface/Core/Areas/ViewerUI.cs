using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

public sealed partial class ViewerUI : AreaUI
{
	public ViewerUI(EchoUI root) : base(root)
	{
		evaluationOperationMode = new EvaluationOperationMode(root);
		staticTextureGridMode = new StaticTextureGridMode(root);
		exploreTextureGridMode = new ExploreTextureGridMode(root);
	}

	Mode currentMode;

	Float2 planeCenter;
	float planeScale = 1f;
	Float2? cursorPosition;

	readonly EvaluationOperationMode evaluationOperationMode;
	readonly StaticTextureGridMode staticTextureGridMode;
	readonly ExploreTextureGridMode exploreTextureGridMode;

	float _logPlaneScale;

	float LogPlaneScale
	{
		get => _logPlaneScale;
		set
		{
			if (_logPlaneScale.Equals(value)) return;

			_logPlaneScale = value;
			planeScale = MathF.Exp(value);
		}
	}

	protected override string Name => "Viewer";

	protected override ImGuiWindowFlags WindowFlags => base.WindowFlags | ImGuiWindowFlags.MenuBar;

	public void Track(EvaluationOperation operation)
	{
		evaluationOperationMode.Reset(operation);
		currentMode = evaluationOperationMode;
	}

	public void Track(TextureGrid texture)
	{
		staticTextureGridMode.Reset(texture);
		currentMode = staticTextureGridMode;
	}

	public void Track(TextureGrid mainTexture, TextureGrid<NormalDepth128> depthTexture, Camera camera)
	{
		exploreTextureGridMode.Reset(mainTexture, depthTexture, camera);
		currentMode = exploreTextureGridMode;
	}

	protected override void NewFrameWindow()
	{
		FindImGuiRegion(out Bounds region);
		UpdateCursorPosition(region);

		if (currentMode != null && ImGui.BeginMenuBar())
		{
			if (ImGui.BeginMenu("View"))
			{
				if (!currentMode.LockPlane && ImGui.MenuItem("Recenter"))
				{
					planeCenter = Float2.Zero;
					LogPlaneScale = 0f;
				}

				if (ImGui.MenuItem("Close")) currentMode = null;

				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}

		if (currentMode != null)
		{
			if (currentMode.LockPlane)
			{
				planeCenter = Float2.Zero;
				LogPlaneScale = 0f;
			}

			DrawMode(region);

			if (ImGui.BeginMenuBar())
			{
				currentMode.DrawMenuBar();
				ImGui.EndMenuBar();
			}
		}

		ProcessMouseInput(region);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposing) return;

		evaluationOperationMode?.Dispose();
		staticTextureGridMode?.Dispose();
		exploreTextureGridMode?.Dispose();
	}

	void DrawMode(in Bounds region)
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(region.MinVector2, region.MaxVector2);

		float planeAspect = currentMode.AspectRatio;
		float regionAspect = region.extend.X / region.extend.Y;

		Float2 displayExtend = planeAspect > regionAspect ?
			new Float2(region.extend.X, region.extend.X / planeAspect) :
			new Float2(region.extend.Y * planeAspect, region.extend.Y);

		Float2? cursorUV = null;

		if (cursorPosition.HasValue)
		{
			Float2 uv = cursorPosition.Value / displayExtend;
			uv = new Float2(-uv.X, uv.Y) / 2f + Float2.Half;
			if (Float2.Zero <= uv && uv < Float2.One) cursorUV = uv;
		}

		Bounds plane = new Bounds(region.center + planeCenter, displayExtend * planeScale);
		if (!currentMode.Draw(drawList, plane, cursorUV)) currentMode = null;

		drawList.PopClipRect();
	}

	void UpdateCursorPosition(in Bounds region)
	{
		//Detect for interaction
		ImGui.SetCursorScreenPos(region.MinVector2);
		ImGui.PushTabStop(false);
		ImGui.InvisibleButton("Viewer", region.extend.AsVector2() * 2f);
		ImGui.PopTabStop();

		if (ImGui.IsItemActive() || ImGui.IsItemHovered())
		{
			//Update cursor position
			Vector2 mousePosition = ImGui.GetIO().MousePos;
			Float2 offset = region.center - mousePosition.AsFloat2();
			cursorPosition = (offset + planeCenter) / planeScale;
		}
		else cursorPosition = null;
	}

	void ProcessMouseInput(in Bounds region)
	{
		if (cursorPosition == null) return;

		ImGuiIOPtr io = ImGui.GetIO();
		float zoom = io.MouseWheel * 0.1f;

		//Zoom towards cursor
		if (!FastMath.AlmostZero(zoom, 0f))
		{
			LogPlaneScale += zoom;

			Float2 offset = region.center - io.MousePos.AsFloat2();
			planeCenter = cursorPosition.Value * planeScale - offset;
		}

		//Move if the left mouse button is held
		if (io.MouseDown[0]) planeCenter += io.MouseDelta.AsFloat2();
	}

	static void FindImGuiRegion(out Bounds region)
	{
		Float2 min = ImGui.GetCursorScreenPos().AsFloat2();
		Float2 max = min + ImGui.GetContentRegionAvail().AsFloat2();
		region = new Bounds((max + min) / 2f, (max - min) / 2f);
	}

	abstract class Mode : IDisposable
	{
		protected Mode(EchoUI root) => this.root = root;

		protected readonly EchoUI root;

		IntPtr display;
		Int2 currentSize;

		/// <summary>
		/// Width over height.
		/// </summary>
		public abstract float AspectRatio { get; }

		/// <summary>
		/// Whether to lock the display plane to the region center.
		/// </summary>
		public virtual bool LockPlane => false;

		ImGuiDevice Backend => root.backend;

		public abstract bool Draw(ImDrawListPtr drawList, in Bounds plane, Float2? cursorUV);

		public virtual void DrawMenuBar() { }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) => Backend.DestroyTexture(ref display);

		~Mode() => Dispose(false);

		protected void DrawDisplay(ImDrawListPtr drawList, in Bounds plane)
		{
			uint borderColor = ImGuiCustom.GetColorInteger(ImGuiCol.Border);
			drawList.AddImage(display, plane.MinVector2, plane.MaxVector2);
			drawList.AddRect(plane.MinVector2, plane.MaxVector2, borderColor);
		}

		protected bool RecreateDisplay(Int2 size)
		{
			if (size == currentSize) return false;
			currentSize = size;

			Backend.DestroyTexture(ref display);
			if (currentSize == Int2.Zero) return true;

			display = Backend.CreateTexture(currentSize, true, false);
			SDL.SDL_SetTextureBlendMode(display, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();
			return true;
		}

		protected void ClearDisplay()
		{
			unsafe
			{
				LockDisplay(out uint* pixels);
				new Span<uint>(pixels, currentSize.Product).Clear();
			}

			UnlockDisplay();
		}

		protected unsafe void LockDisplay(out uint* pixels)
		{
			SDL.SDL_LockTexture(display, IntPtr.Zero, out IntPtr pointer, out int pitch).ThrowOnError();

			pixels = (uint*)pointer;

			if (pitch == sizeof(uint) * currentSize.X) return;
			throw new InvalidOperationException($"Assumption of contiguous memory from {nameof(SDL)} is violated!");
		}

		protected unsafe void LockDisplay(Int2 position, Int2 size, out uint* pixels, out nint stride)
		{
			int invertY = currentSize.Y - (position.Y + size.Y);
			SDL.SDL_Rect rect = new SDL.SDL_Rect { x = position.X, y = invertY, w = size.X, h = size.Y };
			SDL.SDL_LockTexture(display, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

			stride = -pitch / sizeof(uint);
			pixels = (uint*)pointer - position.X - stride * (size.Y - 1);
		}

		protected void UnlockDisplay() => SDL.SDL_UnlockTexture(display);

		protected static uint ColorToUInt32(Float4 color)
		{
			color = ApproximateSqrt(color.Max(Float4.Zero));
			var bytes = ColorConverter.Float4ToBytes(color);
			return ColorConverter.GatherBytes(bytes);

			static Float4 ApproximateSqrt(Float4 value)
			{
				Vector128<float> notZero = Sse.CompareNotEqual(value.v, Vector128<float>.Zero);
				return new Float4(Sse.And(notZero, Sse.Multiply(Sse.ReciprocalSqrt(value.v), value.v)));
			}
		}
	}

	readonly struct Bounds
	{
		public Bounds(Float2 center, Float2 extend)
		{
			Ensure.IsTrue(extend >= Float2.Zero);

			this.center = center;
			this.extend = extend;

			min = center - extend;
			max = center + extend;
		}

		public readonly Float2 center;
		public readonly Float2 extend;

		public readonly Float2 min;
		public readonly Float2 max;

		public Vector2 MinVector2 => min.AsVector2();
		public Vector2 MaxVector2 => max.AsVector2();
	}
}