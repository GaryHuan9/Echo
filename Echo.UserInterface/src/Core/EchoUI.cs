using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Textures.Colors;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Areas;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public sealed class EchoUI : IApplication
{
	public EchoUI()
	{
		var builder = ImmutableArray.CreateBuilder<AreaUI>();

		builder.Add(new SystemUI { Root = this });
		builder.Add(new OperationUI { Root = this });
		builder.Add(new TilesUI { Root = this });
		builder.Add(new LogsUI { Root = this });
		builder.Add(new SceneUI { Root = this });

		areas = builder.ToImmutable();
	}

	readonly ImmutableArray<AreaUI> areas;

	public TimeSpan UpdateDelay { get; set; } = TimeSpan.Zero;
	public string Label => "Echo User Interface";

	public bool RequestTermination { get; private set; }
	public ImGuiDevice Backend { get; private set; }

	public void Initialize(ImGuiDevice backend)
	{
		ImGuiIOPtr io = ImGui.GetIO();

		io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
		io.ConfigWindowsMoveFromTitleBarOnly = true;

		io.Fonts.AddFontFromFileTTF("ext/JetBrainsMono/JetBrainsMono-Bold.ttf", 16f);

		ImGuiStylePtr style = ImGui.GetStyle();

		ConfigureStyle(style);
		ConfigureColors(style);

		Backend = backend;
		foreach (AreaUI area in areas) area.Initialize();
	}

	public void NewFrame(in Moment moment)
	{
		ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

		ImGui.ShowDemoWindow();

		foreach (AreaUI area in areas) area.NewFrame(moment);
	}

	public T Find<T>() where T : AreaUI
	{
		foreach (AreaUI area in areas)
		{
			if (area is T match) return match;
		}

		return null;
	}

	public void Dispose()
	{
		foreach (AreaUI area in areas) area.Dispose();
	}

	static void ConfigureStyle(ImGuiStylePtr style)
	{
		style.WindowPadding = new Vector2(8f, 8f);
		style.FramePadding = new Vector2(4f, 2f);
		style.CellPadding = new Vector2(4f, 2f);
		style.ItemSpacing = new Vector2(8f, 4f);
		style.ItemInnerSpacing = new Vector2(4f, 4f);
		style.TouchExtraPadding = new Vector2(0f, 0f);
		style.IndentSpacing = 20f;
		style.ScrollbarSize = 12f;
		style.GrabMinSize = 8f;

		style.WindowBorderSize = 1f;
		style.ChildBorderSize = 1f;
		style.PopupBorderSize = 1f;
		style.FrameBorderSize = 1f;
		style.TabBorderSize = 1f;

		style.WindowRounding = 1f;
		style.ChildRounding = 1f;
		style.FrameRounding = 1f;
		style.PopupRounding = 1f;
		style.ScrollbarRounding = 1f;
		style.GrabRounding = 1f;
		style.LogSliderDeadzone = 1f;
		style.TabRounding = 1f;

		style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
		style.WindowMenuButtonPosition = ImGuiDir.None;
		style.ColorButtonPosition = ImGuiDir.Left;
		style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
		style.SelectableTextAlign = new Vector2(0f, 0f);
	}

	static void ConfigureColors(ImGuiStylePtr style)
	{
		//Signature Color:
		//0.0250 0.1416 0.3736 (Linear)
		//0.1581 0.3763 0.6112 (Gamma)
		//40.319 95.956 155.86 (Gamma RGB)
		//#28609C              (Gamma Hex)

		var colors = style.Colors;
		const float Alpha = 0.32f;

		RGB128 color = SystemPrng.Shared.Next1(9) switch
		{
			0 or 1 => new RGB128(0.1581f, 0.6112f, 0.3763f), //Green
			2 => (RGB128)RGBA128.Parse("0xDD444C"),          //REDO
			_ => new RGB128(0.1581f, 0.3763f, 0.6112f)       //Signature
		};

		var dark0 = ToVector4(color * 0.33f);
		var dark1 = ToVector4(color * 0.41f);
		var dark2 = ToVector4(color * 0.52f);

		var main0 = ToVector4(color * 1.0f);
		var main1 = ToVector4(color * 1.2f);

		var white = new Vector4(0.94f, 0.96f, 0.99f, 1.00f);
		var black = new Vector4(0.04f, 0.04f, 0.05f, 1.00f);
		var gray = new Vector4(0.51f, 0.54f, 0.60f, Alpha);

		colors[(int)ImGuiCol.Text] = white;
		colors[(int)ImGuiCol.TextDisabled] = white with { W = Alpha };
		colors[(int)ImGuiCol.WindowBg] = black;
		colors[(int)ImGuiCol.PopupBg] = black;
		colors[(int)ImGuiCol.Border] = gray;
		colors[(int)ImGuiCol.FrameBg] = dark0;
		colors[(int)ImGuiCol.FrameBgHovered] = dark1;
		colors[(int)ImGuiCol.FrameBgActive] = dark2;
		colors[(int)ImGuiCol.TitleBg] = dark0;
		colors[(int)ImGuiCol.TitleBgActive] = dark2;
		colors[(int)ImGuiCol.TitleBgCollapsed] = black;
		colors[(int)ImGuiCol.MenuBarBg] = gray;
		colors[(int)ImGuiCol.ScrollbarBg] = Vector4.Zero;
		colors[(int)ImGuiCol.ScrollbarGrab] = dark0;
		colors[(int)ImGuiCol.ScrollbarGrabHovered] = dark1;
		colors[(int)ImGuiCol.ScrollbarGrabActive] = dark2;
		colors[(int)ImGuiCol.CheckMark] = main0;
		colors[(int)ImGuiCol.SliderGrab] = main0;
		colors[(int)ImGuiCol.SliderGrabActive] = main1;
		colors[(int)ImGuiCol.Button] = dark0;
		colors[(int)ImGuiCol.ButtonHovered] = dark1;
		colors[(int)ImGuiCol.ButtonActive] = dark2;
		colors[(int)ImGuiCol.Header] = dark0;
		colors[(int)ImGuiCol.HeaderHovered] = dark1;
		colors[(int)ImGuiCol.HeaderActive] = dark2;
		colors[(int)ImGuiCol.Separator] = dark0;
		colors[(int)ImGuiCol.SeparatorHovered] = dark1;
		colors[(int)ImGuiCol.SeparatorActive] = dark2;
		colors[(int)ImGuiCol.ResizeGrip] = dark0;
		colors[(int)ImGuiCol.ResizeGripHovered] = dark1;
		colors[(int)ImGuiCol.ResizeGripActive] = dark2;
		colors[(int)ImGuiCol.Tab] = dark0;
		colors[(int)ImGuiCol.TabHovered] = dark2;
		colors[(int)ImGuiCol.TabActive] = dark1;
		colors[(int)ImGuiCol.TabUnfocused] = dark0;
		colors[(int)ImGuiCol.TabUnfocusedActive] = dark1;
		colors[(int)ImGuiCol.DockingPreview] = dark2;
		colors[(int)ImGuiCol.DockingEmptyBg] = black;
		colors[(int)ImGuiCol.PlotLines] = main0;
		colors[(int)ImGuiCol.PlotLinesHovered] = main1;
		colors[(int)ImGuiCol.PlotHistogram] = main0;
		colors[(int)ImGuiCol.PlotHistogramHovered] = main1;
		colors[(int)ImGuiCol.TableHeaderBg] = dark0;
		colors[(int)ImGuiCol.TableBorderStrong] = gray;
		colors[(int)ImGuiCol.TableBorderLight] = gray;
		colors[(int)ImGuiCol.TableRowBgAlt] = dark0 with { W = Alpha };
		colors[(int)ImGuiCol.TextSelectedBg] = white with { W = Alpha };
		colors[(int)ImGuiCol.DragDropTarget] = white;
		colors[(int)ImGuiCol.NavHighlight] = white;
		colors[(int)ImGuiCol.NavWindowingHighlight] = white;
		colors[(int)ImGuiCol.NavWindowingDimBg] = white with { W = Alpha };
		colors[(int)ImGuiCol.ModalWindowDimBg] = white with { W = Alpha };

		static Vector4 ToVector4(in RGB128 color) => ((Float4)(RGBA128)color).v.AsVector4();
	}
}