using System;
using System.Collections.Immutable;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Areas;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public sealed class EchoUI : IApplication
{
	public EchoUI()
	{
		var builder = ImmutableArray.CreateBuilder<AreaUI>();

		builder.Add(new SystemUI());
		builder.Add(new OperationUI());
		builder.Add(new TilesUI());
		builder.Add(new ActionsUI());

		areas = builder.ToImmutable();
	}

	readonly ImmutableArray<AreaUI> areas;

	public TimeSpan UpdateDelay => TimeSpan.Zero; //For now this is basically only controlled by vsync
	public bool RequestTermination { get; private set; }
	public string Label => "Echo User Interface";

	public void Initialize()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
		io.ConfigWindowsMoveFromTitleBarOnly = true;

		io.Fonts.AddFontFromFileTTF("Assets/Fonts/JetBrainsMono/JetBrainsMono-Bold.ttf", 16f);

		ImGuiStylePtr style = ImGui.GetStyle();

		foreach (AreaUI area in areas) area.Initialize();
	}

	public void Update()
	{
		DrawMenuBar();
		ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

		ImGui.ShowDemoWindow();

		foreach (AreaUI area in areas) area.Update();
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
}