using System;

namespace Echo.UserInterface.Backend;

public interface IApplication : IDisposable
{
	public string Label { get; }

	public TimeSpan UpdateDelay { get; }

	public void Initialize(ImGuiDevice backend);

	public void NewFrame(in Moment moment);
}