using System;

namespace Echo.UserInterface.Backend;

public interface IApplication : IDisposable
{
	public string Label { get; }

	public TimeSpan UpdateDelay { get; }

	public void NewFrame(in Moment moment);
}