using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute;

/// <summary>
/// A controlled compute engine to execute different <see cref="Operation"/>s.
/// </summary>
public sealed class Device : IDisposable
{
	Device(int population)
	{
		var builder = ImmutableArray.CreateBuilder<Worker>(population);

		for (int i = 0; i < population; i++)
		{
			Worker worker = new Worker(i);

			worker.OnDispatchChangedEvent += OnDispatchChanged;
			worker.OnIdlenessChangedEvent += OnIdlenessChanged;

			builder.Add(worker);
		}

		workers = builder.MoveToImmutable();
	}

	readonly ImmutableArray<Worker> workers;
	readonly List<Operation> operations = new();

	int runningCount;

	readonly Locker manageLocker = new();
	readonly Locker signalLocker = new();
	ReaderWriterLockSlim operationLocker = new();

	/// <summary>
	/// Whether this <see cref="Device"/> is currently idling (ie. not executing any <see cref="Operation"/>).
	/// </summary>
	public bool IsIdle
	{
		get
		{
			using var _ = signalLocker.Fetch();
			Ensure.IsFalse(runningCount < 0);
			return runningCount == 0;
		}
	}

	/// <summary>
	/// The <see cref="IWorker"/>s of this <see cref="Device"/>.
	/// </summary>
	/// <remarks>The <see cref="ImmutableArray{T}.Length"/> of this <see cref="ImmutableArray{T}"/> is identical to <see cref="Population"/>.</remarks>
	public ImmutableArray<IWorker> Workers => workers.CastArray<IWorker>();

	/// <summary>
	/// The number of <see cref="IWorker"/>s this <see cref="Device"/> has.
	/// </summary>
	public int Population => workers.Length;

	/// <summary>
	/// The most recent <see cref="Operation"/> that was dispatched from this <see cref="Device"/>.
	/// </summary>
	public Operation LatestOperation
	{
		get
		{
			operationLocker.EnterReadLock();
			try
			{
				return operations.Count == 0 ? null : operations[^1];
			}
			finally
			{
				operationLocker.ExitReadLock();
			}
		}
	}

	/// <summary>
	/// The <see cref="Operation"/>s that have been dispatched from this <see cref="Device"/> before.
	/// </summary>
	public ReadOnlySpan<Operation> PastOperations
	{
		get
		{
			operationLocker.EnterReadLock();
			try
			{
				return CollectionsMarshal.AsSpan(operations);
			}
			finally
			{
				operationLocker.ExitReadLock();
			}
		}
	}

	int _disposed;

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
			Ensure.IsTrue(Monitor.IsEntered(instanceLocker));
			_instance = value;
		}
	}

	/// <summary>
	/// Blocks the calling thread until this <see cref="Device"/> idles.
	/// </summary>
	/// <seealso cref="IsIdle"/>
	public void AwaitIdle() => AwaitIdleness(true);

	/// <summary>
	/// Begins the execution of a new <see cref="Operation"/>.
	/// </summary>
	/// <param name="factory">The <see cref="IOperationFactory"/> used to create a new <see cref="Operation"/> to execute.</param>
	/// <remarks>If <see cref="LatestOperation"/> is not null, its execution will be prematurely aborted.</remarks>
	public void Dispatch<TFactory>(TFactory factory) where TFactory : IOperationFactory
	{
		ThrowIfDisposed();

		//Create new operation to be dispatched
		Operation operation = factory.CreateOperation(Workers);

		lock (manageLocker)
		{
			//Abort current operation if needed
			if (!IsIdle)
			{
				Abort();
				AwaitIdleness(true);
			}

			//Add to operation history
			operationLocker.EnterWriteLock();
			try
			{
				operations.Add(operation);
			}
			finally
			{
				operationLocker.ExitWriteLock();
			}

			//Dispatch to workers
			foreach (var worker in workers) worker.Dispatch(operation);
		}

		//Blocks until when at least one worker started working
		AwaitIdleness(false);
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

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

		//Unregister from active instance
		lock (instanceLocker)
		{
			Ensure.AreEqual(_instance, this);
			Instance = null;
		}

		//Dispose workers
		lock (manageLocker)
		{
			AbortImpl();
			signalLocker.Signaling = false;
			foreach (var worker in workers) worker.Dispose();
		}

		//Dispose operations
		operationLocker.EnterWriteLock();
		try
		{
			for (int i = 0; i < operations.Count; i++) operations[i].Dispose();
		}
		finally
		{
			operationLocker.ExitWriteLock();
		}

		operationLocker?.Dispose();
		operationLocker = null;
	}

	void AbortImpl()
	{
		using var _ = manageLocker.Fetch();
		foreach (var worker in workers) worker.Abort();
	}

	void AwaitIdleness(bool idle)
	{
		using var _ = signalLocker.Fetch();

		while (idle == runningCount > 0 && !Disposed) signalLocker.Wait();
	}

	void OnDispatchChanged(IWorker worker, bool entered)
	{
		LatestOperation.ChangeWorkerIdleness(worker, !entered);

		if (entered)
		{
			//Just started running
			using var _ = signalLocker.Fetch();
			Ensure.IsFalse(runningCount >= workers.Length);
			++runningCount;

			//Signal if some work is running
			if (runningCount == 1) signalLocker.Signal();
		}
		else
		{
			//Just stopped running
			using var _ = signalLocker.Fetch();
			Ensure.IsFalse(runningCount <= 0);
			--runningCount;

			//Signal if no worker is running
			if (runningCount == 0) signalLocker.Signal();
		}
	}

	void OnIdlenessChanged(IWorker worker, bool entered) => LatestOperation.ChangeWorkerIdleness(worker, entered);

	void ThrowIfDisposed()
	{
		if (Disposed) throw new ObjectDisposedException(nameof(Device));
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

	/// <summary>
	/// Creates or get a <see cref="Device"/>.
	/// </summary>
	/// <returns>The <see cref="Device"/> that was created or retrieved.</returns>
	/// <remarks>This method is similar to atomically checking whether <see cref="Instance"/> is null
	/// and then conditional invoking <see cref="Create"/> if <see cref="Instance"/> is null.</remarks>
	public static Device CreateOrGet()
	{
		using var _ = instanceLocker.Fetch();
		return Instance ?? Create();
	}
}