using System;
using System.Threading;
using CodeHelpers.Diagnostics;
using Echo.Common;
using Echo.Common.Memory;

namespace Echo.Core.Compute;

/// <summary>
/// A controlled compute engine to execute different <see cref="Operation"/>s.
/// </summary>
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

	/// <summary>
	/// Whether this <see cref="Device"/> is currently idling (ie. not executing any <see cref="Operation"/>).
	/// </summary>
	public bool IsIdle
	{
		get
		{
			using var _ = signalLocker.Fetch();
			Assert.IsFalse(runningCount < 0);
			return runningCount == 0;
		}
	}

	/// <summary>
	/// The number of <see cref="Worker"/> for this <see cref="Device"/>.
	/// </summary>
	public int Population => workers.Length;

	/// <summary>
	/// Begins the execution of a new <see cref="Operation"/>.
	/// </summary>
	/// <param name="operation">The <see cref="Operation"/> to execute.</param>
	/// <remarks>If an <see cref="Operation"/> is already dispatched, it will be prematurely aborted.</remarks>
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

	/// <summary>
	/// If an <see cref="Operation"/> is dispatched, pauses it as soon as possible.
	/// </summary>
	public void Pause()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Pause();
	}

	/// <summary>
	/// If a dispatched <see cref="Operation"/> is paused, resumes its execution.
	/// </summary>
	public void Resume()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Resume();
	}

	/// <summary>
	/// If an <see cref="Operation"/> is dispatched, aborts it as soon as possible.
	/// </summary>
	public void Abort()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Abort();
	}

	/// <summary>
	/// Blocks the calling thread until this <see cref="Device"/> idles.
	/// </summary>
	/// <seealso cref="IsIdle"/>
	public void AwaitIdle()
	{
		using var _ = signalLocker.Fetch();

		while (runningCount > 0 && Volatile.Read(ref disposed) == 0)
		{
			signalLocker.Wait();
			Assert.IsFalse(runningCount < 0);
		}
	}

	/// <summary>
	/// Retrieves the <see cref="Worker.State"/> of the <see cref="Worker"/>s in this <see cref="Device"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> which will contain the result.</param>
	public void FillStatuses(ref SpanFill<Worker.State> fill)
	{
		int length = Math.Min(fill.Length, workers.Length);
		for (int i = 0; i < length; i++) fill.Add(workers[i].Status);
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