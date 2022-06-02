using System;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public abstract class AreaUI : IDisposable
{
	protected AreaUI(string name) => this.name = name;

	readonly string name;

	public virtual void Initialize() { }

	public virtual void Update()
	{
		if (ImGui.Begin(name)) Draw();
		ImGui.End();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected abstract void Draw();

	protected virtual void Dispose(bool disposing) { }
}