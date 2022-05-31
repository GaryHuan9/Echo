using System;

namespace Echo.UserInterface.Backend;

public interface IApplication : IDisposable
{
	TimeSpan UpdateDelay { get; }

	string Label { get; }

	void Initialize();

	void Update();
}