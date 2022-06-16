using System;
using System.Numerics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public abstract class PlaneUI : AreaUI
{
	protected PlaneUI(string name) : base(name) { }

	protected Float2 PlaneCenter { get; private set; }
	protected float PlaneScale { get; private set; } = 1f;
	protected Float2? CursorPosition { get; private set; }

	float _logPlaneScale;

	float LogPlaneScale
	{
		get => _logPlaneScale;
		set
		{
			_logPlaneScale = value;
			PlaneScale = MathF.Exp(value);
		}
	}

	protected override void Update(in Moment moment)
	{
		FindCurrentRegion(out Bounds region);
		UpdateCursorPosition(region);

		var drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(region.MinVector2, region.MaxVector2);

		Draw(drawList, region);
		drawList.PopClipRect();
		ProcessMouseInput(region);
	}

	protected abstract void Draw(ImDrawListPtr drawList, in Bounds region);

	protected void Recenter()
	{
		LogPlaneScale = 0f;
		PlaneCenter = Float2.Zero;
	}

	void UpdateCursorPosition(in Bounds region)
	{
		//Detect for interaction
		ImGui.SetCursorScreenPos(region.MinVector2);
		ImGui.PushAllowKeyboardFocus(false);
		ImGui.InvisibleButton("Plane", region.extend.AsVector2() * 2f);
		ImGui.PopAllowKeyboardFocus();

		if (ImGui.IsItemActive() || ImGui.IsItemHovered())
		{
			//Update cursor position
			Vector2 mousePosition = ImGui.GetIO().MousePos;
			Float2 offset = region.center - mousePosition.AsFloat2();
			CursorPosition = (offset + PlaneCenter) / PlaneScale;
		}
		else CursorPosition = null;
	}

	void ProcessMouseInput(in Bounds region)
	{
		if (CursorPosition == null) return;

		ImGuiIOPtr io = ImGui.GetIO();
		float zoom = io.MouseWheel * 0.1f;

		//Zoom towards cursor
		if (zoom != 0f)
		{
			LogPlaneScale += zoom;

			Float2 offset = region.center - io.MousePos.AsFloat2();
			PlaneCenter = CursorPosition.Value * PlaneScale - offset;
		}

		//Move if the left mouse button is held
		if (io.MouseDown[0]) PlaneCenter += io.MouseDelta.AsFloat2();
	}

	/// <summary>
	/// Find the <see cref="Bounds"/> for the current region.
	/// </summary>
	static void FindCurrentRegion(out Bounds region)
	{
		Float2 min = ImGui.GetCursorScreenPos().AsFloat2();
		Float2 max = min + ImGui.GetContentRegionAvail().AsFloat2();
		region = new Bounds((max + min) / 2f, (max - min) / 2f);
	}

	protected readonly struct Bounds
	{
		public Bounds(Float2 center, Float2 extend)
		{
			Assert.IsTrue(extend >= Float2.Zero);

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