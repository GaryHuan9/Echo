using System;
using System.Threading;

namespace Echo.Core.Compute;

public sealed class Scheduler : IDisposable
{
	public Scheduler(int id) => this.id = id;

	/// <summary>
	/// Two <see cref="Scheduler"/> with the same <see cref="id"/> will never execute at the same time.
	/// </summary>
	public readonly int id;

	readonly ManualResetEvent pauseEvent = new(true);

	public void CheckSchedule()
	{
		pauseEvent.WaitOne();
		//TODO: check for abortion
	}

	public void Dispose() => pauseEvent?.Dispose();
}