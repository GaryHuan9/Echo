using System;
using System.Threading;
using CodeHelpers.Diagnostics;
using Echo.Common;

namespace Echo.Core.Compute;

public sealed class Device : IDisposable
{
	public Device() : this(Environment.ProcessorCount) { }

	public Device(int population)
	{
		workers = new Worker[population];

		for (int i = 0; i < workers.Length; i++)
		{
			ref Worker worker = ref workers[i];
			worker = new Worker((uint)i);

			worker.OnRunEvent += OnWorkerRun;
			worker.OnIdleEvent += OnWorkerIdle;
		}
	}

	readonly Worker[] workers;

	int runningCount;
	int disposed;

	readonly Locker manageLocker = new();
	readonly Locker signalLocker = new();

	public bool IsIdle
	{
		get
		{
			using var _ = signalLocker.Fetch();
			Assert.IsFalse(runningCount < 0);
			return runningCount == 0;
		}
	}

	public void AwaitIdle()
	{
		using var _ = signalLocker.Fetch();

		while (runningCount > 0 && Volatile.Read(ref disposed) == 0)
		{
			signalLocker.Wait();
			Assert.IsFalse(runningCount < 0);
		}
	}

	public void Dispatch(Operation operation)
	{
		operation.Prepare(workers.Length);
		using var _ = manageLocker.Fetch();

		if (!IsIdle)
		{
			Abort();
			AwaitIdle();
		}

		foreach (var worker in workers) worker.Dispatch(operation);
	}

	public void Pause()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Pause();
	}

	public void Resume()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Resume();
	}

	public void Abort()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Abort();
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 1) return;

		Abort();

		using var _ = manageLocker.Fetch();
		signalLocker.Signaling = false;

		foreach (var worker in workers) worker.Dispose();
	}

	void OnWorkerRun(Worker worker)
	{
		using var _ = signalLocker.Fetch();
		Assert.IsFalse(runningCount >= workers.Length);
		++runningCount;
	}

	void OnWorkerIdle(Worker worker)
	{
		using var _ = signalLocker.Fetch();
		Assert.IsFalse(runningCount <= 0);
		--runningCount;

		//Signal if all workers are idle
		if (runningCount == 0) signalLocker.Signal();
	}
}