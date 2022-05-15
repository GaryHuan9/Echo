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
	Device(int population)
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

	int _disposed;

	/// <summary>
	/// Whether this <see cref="Device"/> is disposed.
	/// Disposed <see cref="Device"/> should not be used.
	/// </summary>
	public bool Disposed => Volatile.Read(ref _disposed) != 0;

	static readonly Locker instanceLocker = new();
	static Device _instance;

	/// <summary>
	/// The current valid (non-disposed) <see cref="Device"/> in this <see cref="AppDomain"/>.
	/// </summary>
	/// <seealso cref="Create"/>
	public static Device Instance
	{
		get
		{
			using var _ = instanceLocker.Fetch();

			Device instance = _instance;
			if (instance == null) return null;
			if (!instance.Disposed) return instance;

			_instance = null;
			return null;
		}
		private set
		{
			Assert.IsTrue(Monitor.IsEntered(instanceLocker));
			_instance = value;
		}
	}

	/// <summary>
	/// Begins the execution of a new <see cref="Operation"/>.
	/// </summary>
	/// <param name="operation">The <see cref="Operation"/> to execute.</param>
	/// <remarks>If an <see cref="Operation"/> is already dispatched, it will be prematurely aborted.</remarks>
	public void Dispatch(Operation operation)
	{
		ThrowIfDisposed();

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
		ThrowIfDisposed();

		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Pause();
	}

	/// <summary>
	/// If a dispatched <see cref="Operation"/> is paused, resumes its execution.
	/// </summary>
	public void Resume()
	{
		ThrowIfDisposed();

		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Resume();
	}

	/// <summary>
	/// If an <see cref="Operation"/> is dispatched, aborts it as soon as possible.
	/// </summary>
	public void Abort()
	{
		ThrowIfDisposed();
		AbortImpl();
	}

	/// <summary>
	/// Blocks the calling thread until this <see cref="Device"/> idles.
	/// </summary>
	/// <seealso cref="IsIdle"/>
	public void AwaitIdle()
	{
		using var _ = signalLocker.Fetch();

		while (runningCount > 0 && !Disposed)
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
		if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

		AbortImpl();

		using var _ = manageLocker.Fetch();
		signalLocker.Signaling = false;

		foreach (var worker in workers) worker.Dispose();
	}

	void AbortImpl()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Abort();
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

	void ThrowIfDisposed()
	{
		if (!Disposed) return;
		throw new ObjectDisposedException(nameof(Device));
	}

	/// <summary>
	/// Creates a new <see cref="Device"/>.
	/// </summary>
	/// <returns>The <see cref="Device"/> that was created.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Instance"/> is not null.</exception>
	/// <remarks>Only one non-disposed <see cref="Device"/> can exist within one <see cref="AppDomain"/>. The current
	/// valid <see cref="Device"/> can be accesses through the <see cref="Instance"/> static property.</remarks>
	/// <seealso cref="Instance"/>
	public static Device Create()
	{
		using var _ = instanceLocker.Fetch();

		Device instance = Instance;
		if (instance != null) throw new InvalidOperationException($"Only one {nameof(Device)} can exist within one {nameof(AppDomain)}.");

		return Instance = new Device(Environment.ProcessorCount);
	}
}