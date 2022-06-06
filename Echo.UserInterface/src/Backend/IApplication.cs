using System;

namespace Echo.UserInterface.Backend;

public interface IApplication : IDisposable
{
	string Label { get; }

	TimeSpan UpdateDelay { get; }

	bool RequestTermination { get; }

	void Initialize(ImGuiDevice backend);

	void Update(in Moment moment);
}