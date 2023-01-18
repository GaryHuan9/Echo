using System;
using System.Numerics;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Processes.Evaluation;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public partial class ViewerUI : AreaUI
{
	public ViewerUI(EchoUI root) : base(root) => evaluationOperationMode = new EvaluationOperationMode(root);

	Mode currentMode;

	Float2 planeCenter;
	float planeScale = 1f;
	Float2? cursorPosition;

	readonly EvaluationOperationMode evaluationOperationMode;

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

	public override string Name => "Viewer";

	public void Track(EvaluationOperation operation)
	{
		evaluationOperationMode.Reset(operation);
		currentMode = evaluationOperationMode;
	}

	public override void NewFrame(in Moment moment)
	{
		FindImGuiRegion(out Bounds region);
		UpdateCursorPosition(region);

		if (currentMode != null) DrawModePlane(region, currentMode);

		ProcessMouseInput(region);
	}

	void DrawModePlane(in Bounds region, Mode mode)
	{
		Ensure.IsNotNull(mode);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(region.MinVector2, region.MaxVector2);

		float planeAspect = mode.AspectRatio;
		float regionAspect = region.extend.X / region.extend.Y;

		Float2 displayExtend = planeAspect > regionAspect
			? new Float2(region.extend.X, region.extend.X / planeAspect)
			: new Float2(region.extend.Y * planeAspect, region.extend.Y);

		Bounds plane = new Bounds(region.center + planeCenter, displayExtend * planeScale);

		currentMode.DrawPlane(drawList, plane);
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
		protected Mode(EchoUI root) => backend = root.backend;

		readonly ImGuiDevice backend;

		IntPtr _display;
		Int2 currentSize;

		protected IntPtr Display => _display;

		/// <summary>
		/// Width over height.
		/// </summary>
		public abstract float AspectRatio { get; }

		public abstract void DrawPlane(ImDrawListPtr drawList, in Bounds plane);

		protected void RecreateDisplay(Int2 size)
		{
			if (size == currentSize) return;
			currentSize = size;

			backend.DestroyTexture(ref _display);
			if (currentSize == Int2.Zero) return;
			_display = backend.CreateTexture(currentSize, true);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			backend.DestroyTexture(ref _display);
		}

		~Mode() => Dispose();
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