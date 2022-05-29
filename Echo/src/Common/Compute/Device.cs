using System;
using System.Threading;
using CodeHelpers.Diagnostics;

namespace Echo.Common.Compute;

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

			worker.OnIdleChangedEvent += OnIdleChanged;
			worker.OnAwaitChangedEvent += OnAwaitChanged;
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
	/// The number of <see cref="Worker"/>s for this <see cref="Device"/>.
	/// </summary>
	public int Population => workers.Length;

	/// <summary>
	/// A <see cref="ReadOnlySpan{T}"/> of <see cref="IWorker"/>s representing
	/// the <see cref="Worker"/>s of this <see cref="Device"/>.
	/// </summary>
	/// <remarks>The <see cref="ReadOnlySpan{T}.Length"/> of the returned
	/// <see cref="ReadOnlySpan{T}"/> is the same as <see cref="Population"/>.</remarks>
	public ReadOnlySpan<IWorker> Workers => workers;

	Operation _startedOperation;
	int _disposed;

	/// <summary>
	/// The last <see cref="Operation"/> that was dispatched.
	/// </summary>
	/// <remarks>This includes completed <see cref="Operation"/>s.</remarks>
	public Operation StartedOperation => _startedOperation;

	/// <summary>
	/// The progress on the <see cref="StartedOperation"/>.
	/// </summary>
	/// <remarks>This value is between zero and one (both inclusive).</remarks>
	public double StartedProgress
	{
		get
		{
			Operation operation = StartedOperation;
			if (operation == null) return 0d;

			double progress = 0d;

			if (!IsIdle)
			{
				//If the device is idle, all workers should have a progress of zero
				foreach (Worker worker in workers) progress += worker.Progress;
			}

			progress += operation.CompletedProcedureCount;
			return progress / operation.TotalProcedureCount;
		}
	}

	/// <summary>
	/// Whether this <see cref="Device"/> is disposed.
	/// Disposed <see cref="Device"/> should not be used.
	/// </summary>
	public bool Disposed => _disposed != 0;

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

		Interlocked.Exchange(ref _startedOperation, operation);
		foreach (var worker in workers) worker.Dispatch(operation);

		AwaitState(false); //Returns from method when at least one worker started working
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
	public void AwaitIdle() => AwaitState(true);

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

	void AwaitState(bool idle)
	{
		using var _ = signalLocker.Fetch();

		while (idle == runningCount > 0 && !Disposed) signalLocker.Wait();
	}

	void OnIdleChanged(Worker worker, bool entered)
	{
		using var _ = signalLocker.Fetch();

		if (entered)
		{
			//Just entered idle
			Assert.IsFalse(runningCount <= 0);
			--runningCount;

			//Signal if all workers are idle
			if (runningCount == 0) signalLocker.Signal();
		}
		else
		{
			//Just exited idle
			Assert.IsFalse(runningCount >= workers.Length);
			++runningCount;

			//Signal if not all workers are idle
			if (runningCount == 1) signalLocker.Signal();
		}
	}

	void OnAwaitChanged(Worker worker, bool entered) { }

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

		if (Instance == null) return Instance = new Device(Environment.ProcessorCount);
		throw new InvalidOperationException($"Only one {nameof(Device)} can exist within one {nameof(AppDomain)}.");
	}
}