using System;
using Echo.UserInterface.Backend;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public abstract class AreaUI : IDisposable
{
	protected AreaUI(EchoUI root) => this.root = root;

	protected readonly EchoUI root;

	protected abstract string Name { get; }

	protected virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

	public virtual void Initialize() { }

	public virtual void NewFrame()
	{
		if (ImGui.Begin(Name, WindowFlags)) NewFrameWindow();
		ImGui.End();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected abstract void NewFrameWindow();

	protected virtual void Dispose(bool disposing) { }
}