using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed partial class ViewerUI : AreaUI
{
	public ViewerUI(EchoUI root) : base(root)
	{
		evaluationOperationMode = new EvaluationOperationMode(root);
		staticTextureGridMode = new StaticTextureGridMode(root);
	}

	Mode currentMode;

	Float2 planeCenter;
	float planeScale = 1f;
	Float2? cursorPosition;

	readonly EvaluationOperationMode evaluationOperationMode;
	readonly StaticTextureGridMode staticTextureGridMode;

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

	protected override void NewFrameWindow(in Moment moment)
	{
		FindImGuiRegion(out Bounds region);
		UpdateCursorPosition(region);

		if (currentMode != null && ImGui.BeginMenuBar())
		{
			if (ImGui.BeginMenu("View"))
			{
				if (ImGui.MenuItem("Recenter"))
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
	}

	void DrawMode(in Bounds region)
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(region.MinVector2, region.MaxVector2);

		float planeAspect = currentMode.AspectRatio;
		float regionAspect = region.extend.X / region.extend.Y;

		Float2 displayExtend = planeAspect > regionAspect
			? new Float2(region.extend.X, region.extend.X / planeAspect)
			: new Float2(region.extend.Y * planeAspect, region.extend.Y);

		Bounds plane = new Bounds(region.center + planeCenter, displayExtend * planeScale);

		if (cursorPosition.HasValue)
		{
			Float2 uv = cursorPosition.Value / displayExtend;
			uv = new Float2(-uv.X, uv.Y) / 2f + Float2.Half;
			currentMode.Draw(drawList, plane, Float2.Zero <= uv && uv < Float2.One ? uv : null);
		}
		else currentMode.Draw(drawList, plane, null);

		drawList.PopClipRect();
	}

	void UpdateCursorPosition(in Bounds region)
	{
		//Detect for interaction
		ImGui.SetCursorScreenPos(region.MinVector2);
		ImGui.PushAllowKeyboardFocus(false);
		ImGui.InvisibleButton("Viewer", region.extend.AsVector2() * 2f);
		ImGui.PopAllowKeyboardFocus();

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

		IntPtr _display;
		Int2 currentSize;

		/// <summary>
		/// Width over height.
		/// </summary>
		public abstract float AspectRatio { get; }

		protected IntPtr Display => _display;
		ImGuiDevice Backend => root.backend;

		public abstract void Draw(ImDrawListPtr drawList, in Bounds plane, Float2? cursorUV);

		public abstract void DrawMenuBar();

		protected void RecreateDisplay(Int2 size)
		{
			if (size == currentSize) return;
			currentSize = size;

			Backend.DestroyTexture(ref _display);
			if (currentSize == Int2.Zero) return;
			_display = Backend.CreateTexture(currentSize, true);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Backend.DestroyTexture(ref _display);
		}

		~Mode() => Dispose();

		protected static uint ColorToUInt32(Float4 color)
		{
			color = ApproximateSqrt(color.Max(Float4.Zero));
			color = color.Min(Float4.One) * byte.MaxValue;
			return ColorConverter.GatherBytes(Sse2.ConvertToVector128Int32(color.v).AsUInt32());

			static Float4 ApproximateSqrt(in Float4 value)
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