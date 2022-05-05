using System;

namespace Echo.Core.Compute;

public sealed class Scheduler : IDisposable
{
	public Scheduler(int id) => this.id = id;

	public readonly int id;



	public void Dispose() { }
}