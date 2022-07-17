using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics.Randomization;
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

		RGB128 color = SystemPrng.Shared.Next1(16) switch
		{
			0 or 1 => new RGB128(0.1581f, 0.6112f, 0.3763f), //Green
			2 or 3 => (RGB128)RGBA128.Parse("#FA983A"),      //Orange
			4      => (RGB128)RGBA128.Parse("#DD444C"),      //REDO
			_      => new RGB128(0.1581f, 0.3763f, 0.6112f)  //Signature
		};

		const float Alpha0 = 0.33f;
		const float Alpha1 = 0.61f;

		var main = ToVector4(color * 1.0f);
		var white1 = ToVector4(RGBA128.Parse("#FAFBFF"));
		var white0 = Vector4.Lerp(main, white1, Alpha1);
		var background0 = ToVector4(RGBA128.Parse("#141517"));
		var background1 = ToVector4(RGBA128.Parse("#23272E"));
		var contrast = ToVector4(RGBA128.Parse("#373B3F"));

		colors[(int)ImGuiCol.Text] = white1;
		colors[(int)ImGuiCol.TextDisabled] = white0;
		colors[(int)ImGuiCol.WindowBg] = background0;
		colors[(int)ImGuiCol.PopupBg] = background0;
		colors[(int)ImGuiCol.Border] = main with { W = Alpha1 };
		colors[(int)ImGuiCol.FrameBg] = Vector4.Zero;
		colors[(int)ImGuiCol.FrameBgHovered] = contrast;
		colors[(int)ImGuiCol.FrameBgActive] = main;
		colors[(int)ImGuiCol.TitleBg] = background0;
		colors[(int)ImGuiCol.TitleBgActive] = contrast;
		colors[(int)ImGuiCol.TitleBgCollapsed] = background0;
		colors[(int)ImGuiCol.MenuBarBg] = background1;
		colors[(int)ImGuiCol.ScrollbarBg] = Vector4.Zero;
		colors[(int)ImGuiCol.ScrollbarGrab] = background1;
		colors[(int)ImGuiCol.ScrollbarGrabHovered] = contrast;
		colors[(int)ImGuiCol.ScrollbarGrabActive] = main;
		colors[(int)ImGuiCol.CheckMark] = main;
		colors[(int)ImGuiCol.SliderGrab] = main;
		colors[(int)ImGuiCol.SliderGrabActive] = white0;
		colors[(int)ImGuiCol.Button] = Vector4.Zero;
		colors[(int)ImGuiCol.ButtonHovered] = contrast;
		colors[(int)ImGuiCol.ButtonActive] = main;
		colors[(int)ImGuiCol.Header] = Vector4.Zero;
		colors[(int)ImGuiCol.HeaderHovered] = contrast;
		colors[(int)ImGuiCol.HeaderActive] = main;
		colors[(int)ImGuiCol.Separator] = background1;
		colors[(int)ImGuiCol.SeparatorHovered] = contrast;
		colors[(int)ImGuiCol.SeparatorActive] = main;
		colors[(int)ImGuiCol.ResizeGrip] = Vector4.Zero;
		colors[(int)ImGuiCol.ResizeGripHovered] = Vector4.Zero;
		colors[(int)ImGuiCol.ResizeGripActive] = Vector4.Zero;
		colors[(int)ImGuiCol.Tab] = background0;
		colors[(int)ImGuiCol.TabHovered] = main;
		colors[(int)ImGuiCol.TabActive] = main;
		colors[(int)ImGuiCol.TabUnfocused] = background0;
		colors[(int)ImGuiCol.TabUnfocusedActive] = contrast;
		colors[(int)ImGuiCol.DockingPreview] = contrast;
		colors[(int)ImGuiCol.DockingEmptyBg] = background0;
		colors[(int)ImGuiCol.PlotLines] = main;
		colors[(int)ImGuiCol.PlotLinesHovered] = white0;
		colors[(int)ImGuiCol.PlotHistogram] = main;
		colors[(int)ImGuiCol.PlotHistogramHovered] = white0;
		colors[(int)ImGuiCol.TableHeaderBg] = background1;
		colors[(int)ImGuiCol.TableBorderStrong] = main with { W = Alpha1 };
		colors[(int)ImGuiCol.TableBorderLight] = main with { W = Alpha1 };
		colors[(int)ImGuiCol.TableRowBgAlt] = background1 with { W = Alpha0 };
		colors[(int)ImGuiCol.TextSelectedBg] = white1 with { W = Alpha0 };
		colors[(int)ImGuiCol.DragDropTarget] = white1 with { W = Alpha1 };
		colors[(int)ImGuiCol.NavHighlight] = white1 with { W = Alpha1 };
		colors[(int)ImGuiCol.NavWindowingHighlight] = white1 with { W = Alpha1 };
		colors[(int)ImGuiCol.NavWindowingDimBg] = white1 with { W = Alpha0 };
		colors[(int)ImGuiCol.ModalWindowDimBg] = white1 with { W = Alpha0 };
	}

	static Vector4 ToVector4(in RGB128 color) => ToVector4((RGBA128)color);
	static Vector4 ToVector4(in RGBA128 color) => ((Float4)color).v.AsVector4();
}