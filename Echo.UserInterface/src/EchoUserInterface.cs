using System;
using Echo.UserInterface.Backend;
using ImGuiNET;

namespace Echo.UserInterface;

public sealed class EchoUserInterface : IApplication
{
	public TimeSpan UpdateDelay { get; } = TimeSpan.FromSeconds(1f / 60f);

	public string Label => "Echo User Interface";

	public void Initialize() { }

	public void Update()
	{
		// if (ImGui.Begin("Test Window")) { }
		ImGuiIOPtr io = ImGui.GetIO();

		ImGui.ShowDemoWindow();
	}

	public void Dispose() { }
}