using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Areas;
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
		DrawMenuBar();
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

	void DrawMenuBar()
	{
		if (ImGui.BeginMainMenuBar())
		{
			ImGui.TextDisabled("Echo User Interface");

			if (ImGui.MenuItem("Quit")) RequestTermination = true;

			ImGui.EndMainMenuBar();
		}
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
		style.GrabMinSize = 10f;

		style.WindowBorderSize = 1f;
		style.ChildBorderSize = 1f;
		style.PopupBorderSize = 1f;
		style.FrameBorderSize = 1f;
		style.TabBorderSize = 1f;

		style.WindowRounding = 2f;
		style.ChildRounding = 2f;
		style.FrameRounding = 2f;
		style.PopupRounding = 2f;
		style.ScrollbarRounding = 2f;
		style.GrabRounding = 2f;
		style.LogSliderDeadzone = 2f;
		style.TabRounding = 2f;

		style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
		style.WindowMenuButtonPosition = ImGuiDir.Right;
		style.ColorButtonPosition = ImGuiDir.Right;
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
		const float Alpha = 0.27f;

		var signature = new RGB128(0.1581f, 0.3763f, 0.6112f);

		var highlight = new Vector4(0.94f, 0.96f, 0.99f, 1.00f);
		var background = new Vector4(0.04f, 0.04f, 0.05f, 1.00f);
		var border = new Vector4(0.43f, 0.47f, 0.59f, Alpha);

		var contrast0 = new Vector4(0.39f, 0.51f, 0.63f, 1.00f);
		var contrast1 = new Vector4(0.59f, 0.71f, 0.82f, 1.00f);

		var main0 = ToVector4(signature / 3.01f);
		var main1 = ToVector4(signature / 2.43f);
		var main2 = ToVector4(signature / 1.72f);

		colors[(int)ImGuiCol.Text] = highlight;
		colors[(int)ImGuiCol.TextDisabled] = contrast1;
		colors[(int)ImGuiCol.WindowBg] = background;
		colors[(int)ImGuiCol.PopupBg] = background;
		colors[(int)ImGuiCol.Border] = border;
		colors[(int)ImGuiCol.FrameBg] = main0;
		colors[(int)ImGuiCol.FrameBgHovered] = main1;
		colors[(int)ImGuiCol.FrameBgActive] = main2;
		colors[(int)ImGuiCol.TitleBg] = main0;
		colors[(int)ImGuiCol.TitleBgActive] = main2;
		colors[(int)ImGuiCol.TitleBgCollapsed] = background;
		colors[(int)ImGuiCol.MenuBarBg] = main1;
		colors[(int)ImGuiCol.ScrollbarBg] = Vector4.Zero;
		colors[(int)ImGuiCol.ScrollbarGrab] = main0;
		colors[(int)ImGuiCol.ScrollbarGrabHovered] = main1;
		colors[(int)ImGuiCol.ScrollbarGrabActive] = main2;
		colors[(int)ImGuiCol.CheckMark] = contrast1;
		colors[(int)ImGuiCol.SliderGrab] = contrast0;
		colors[(int)ImGuiCol.SliderGrabActive] = contrast1;
		colors[(int)ImGuiCol.Button] = main0;
		colors[(int)ImGuiCol.ButtonHovered] = main1;
		colors[(int)ImGuiCol.ButtonActive] = main2;
		colors[(int)ImGuiCol.Header] = main0;
		colors[(int)ImGuiCol.HeaderHovered] = main1;
		colors[(int)ImGuiCol.HeaderActive] = main2;
		colors[(int)ImGuiCol.Separator] = main0;
		colors[(int)ImGuiCol.SeparatorHovered] = main1;
		colors[(int)ImGuiCol.SeparatorActive] = main2;
		colors[(int)ImGuiCol.ResizeGrip] = main0;
		colors[(int)ImGuiCol.ResizeGripHovered] = main1;
		colors[(int)ImGuiCol.ResizeGripActive] = main2;
		colors[(int)ImGuiCol.Tab] = main0;
		colors[(int)ImGuiCol.TabHovered] = main2;
		colors[(int)ImGuiCol.TabActive] = main1;
		colors[(int)ImGuiCol.TabUnfocused] = main0;
		colors[(int)ImGuiCol.TabUnfocusedActive] = main1;
		colors[(int)ImGuiCol.DockingPreview] = main2;
		colors[(int)ImGuiCol.DockingEmptyBg] = background;
		colors[(int)ImGuiCol.PlotLines] = contrast0;
		colors[(int)ImGuiCol.PlotLinesHovered] = contrast1;
		colors[(int)ImGuiCol.PlotHistogram] = contrast0;
		colors[(int)ImGuiCol.PlotHistogramHovered] = contrast1;
		colors[(int)ImGuiCol.TableHeaderBg] = main0;
		colors[(int)ImGuiCol.TableBorderStrong] = border;
		colors[(int)ImGuiCol.TableBorderLight] = border;
		colors[(int)ImGuiCol.TableRowBgAlt] = main0 with { W = Alpha };
		colors[(int)ImGuiCol.TextSelectedBg] = highlight with { W = Alpha };
		colors[(int)ImGuiCol.DragDropTarget] = highlight;
		colors[(int)ImGuiCol.NavHighlight] = highlight;
		colors[(int)ImGuiCol.NavWindowingHighlight] = highlight;
		colors[(int)ImGuiCol.NavWindowingDimBg] = highlight with { W = Alpha };
		colors[(int)ImGuiCol.ModalWindowDimBg] = highlight with { W = Alpha };

		static Vector4 ToVector4(in RGB128 color) => ((Float4)(RGBA128)color).v.AsVector4();
	}
}