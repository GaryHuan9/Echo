using System;
using Echo.UserInterface.Backend;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public abstract class AreaUI : IDisposable
{
	protected AreaUI(string name) => this.name = name;

	readonly string name;

	public EchoUI Root { get; init; }

	public virtual void Initialize() { }

	public virtual void NewFrame(in Moment moment)
	{
		if (ImGui.Begin(name)) Update(moment);
		ImGui.End();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected abstract void Update(in Moment moment);

	protected virtual void Dispose(bool disposing) { }
}