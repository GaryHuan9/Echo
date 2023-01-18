using System;
using Echo.UserInterface.Backend;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public abstract class AreaUI : IDisposable
{
	protected AreaUI(EchoUI root) => this.root = root;

	protected readonly EchoUI root;

	public abstract string Name { get; }

	public virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

	public virtual void Initialize() { }

	public abstract void NewFrame(in Moment moment);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) { }
}