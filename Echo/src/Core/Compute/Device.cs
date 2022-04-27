using System;
using System.Threading;
using CodeHelpers.Diagnostics;

namespace Echo.Core.Compute;

public sealed partial class Device : IDisposable
{
	public Device(int population)
	{
		guid = Guid.NewGuid();
		threads = new Thread[population];
	}

	readonly Guid guid;
	readonly Thread[] threads;

	readonly object fieldsLocker = new();
	readonly object manageLocker = new();
	readonly object signalLocker = new();

	public int Population => threads.Length;
	public State Status { get; private set; }

	public bool Paused { get; set; }

	public void Dispatch(Operation operation)
	{
		operation.Validate();

		Abandon();
		AwaitIdle();

		foreach (Thread thread in threads)
		{

		}
	}

	public void Abandon() { }

	public void Abort() { }

	public void AwaitIdle() { }

	public void Dispose()
	{
		Abort();
	}

	void CreateThread(int index)
	{
		ref Thread thread = ref threads[index];
		Assert.IsNull(thread);

		thread = new Thread(Main)
		{
			IsBackground = true, Priority = ThreadPriority.AboveNormal,
			Name = $"{nameof(Device)} {guid} {nameof(Thread)} {index}"
		};

		thread.Start();
	}

	void Main()
	{
		while (true)
		{
			try
			{

			}
			catch (OperationAbortedException)
			{
				throw;
			}
		}
	}

	public enum State
	{
		Idle,
		Running,
		Paused,
		Disposed
	}

}