using System;
using ImGuiNET;

namespace Echo.UserInterface;

public sealed class Application : IDisposable
{
	public void Initialize() { }

	public void Update()
	{
		// if (ImGui.Begin("Test Window")) { }
		ImGuiIOPtr io = ImGui.GetIO();

		ImGui.ShowDemoWindow();
	}

	public void Dispose() { }
}