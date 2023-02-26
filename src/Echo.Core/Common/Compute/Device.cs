using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Compute;

/// <summary>
/// A controlled compute engine to execute different <see cref="Operation"/>s.
/// </summary>
public sealed class Device : IDisposable
{
	/// <summary>
	/// Creates a new <see cref="Device"/>.
	/// </summary>
	/// <param name="utilization">A normalized number (between 0 and 1) that determines the <see cref="Population"/>.</param>
	/// <remarks>Usually only one <see cref="Device"/> is needed per program.</remarks>
	public Device(float utilization = 1f) : this
	(
		(utilization * Environment.ProcessorCount)
	   .Round().Clamp(1, Environment.ProcessorCount)
	) { }

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
	readonly OperationsQueue operations = new();

	uint dispatchPositives; //The number of workers received a dispatch to work on an operation
	uint dispatchNegatives; //The number of workers ended the dispatch they received
	readonly Locker locker = new();

	/// <summary>
	/// Whether this <see cref="Device"/> has a currently dispatched <see cref="Operation"/>.
	/// </summary>
	public bool IsDispatched => dispatchPositives > 0;

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
	/// An interface to access all past, present, and queued operations designated to this <see cref="Device"/>.
	/// </summary>
	public IOperations Operations => operations;

	int _disposed;

	/// <summary>
	/// Whether this <see cref="Device"/> is disposed.
	/// Disposed <see cref="Device"/> should not be used.
	/// </summary>
	public bool Disposed => _disposed != 0;

	/// <summary>
	/// Queues the execution of a new <see cref="Operation"/>.
	/// </summary>
	/// <param name="factory">The <see cref="IOperationFactory"/> used to create a new <see cref="Operation"/> to execute.</param>
	/// <returns>The newly created <see cref="Operation"/> that was enqueued.</returns>
	/// <remarks>This <see cref="Device"/> will begin working on the enqueued <see cref="Operation"/>
	/// once all previous <see cref="Operation"/>s are either completed or aborted.</remarks>
	public Operation Schedule<TFactory>(TFactory factory) where TFactory : IOperationFactory
	{
		ThrowIfDisposed();

		//Create new operation to be scheduled
		Operation operation = factory.CreateOperation(Workers);

		if (operations.Enqueue(operation))
		{
			//Add to operations queue
			using var _ = locker.Fetch();
			Dispatch(operation);
		}

		return operation;
	}

	/// <summary>
	/// If <see cref="IsDispatched"/>, pause the dispatched <see cref="Operation"/> as soon as possible.
	/// </summary>
	public void Pause()
	{
		using var _ = locker.Fetch();
		ThrowIfDisposed();
		foreach (var worker in workers) worker.Pause();
	}

	/// <summary>
	/// If <see cref="IsDispatched"/> and the dispatched <see cref="Operation"/> is paused, resume its execution.
	/// </summary>
	public void Resume()
	{
		using var _ = locker.Fetch();
		ThrowIfDisposed();
		foreach (var worker in workers) worker.Resume();
	}

	/// <summary>
	/// If <see cref="IsDispatched"/>, abort the dispatched <see cref="Operation"/> as soon as possible.
	/// </summary>
	public void Abort()
	{
		using var _ = locker.Fetch();
		ThrowIfDisposed();
		foreach (var worker in workers) worker.Abort();
	}

	/// <summary>
	/// Aborts or skips an <see cref="Operation"/> scheduled on this device.
	/// </summary>
	/// <param name="operation">The <see cref="Operation"/> to abort or skip.</param>
	/// <exception cref="ArgumentOutOfRangeException">Argument is not an <see cref="Operation"/> scheduled to this <see cref="Device"/>.</exception>
	public void Abort(Operation operation)
	{
		int index = operations.IndexOf(operation);
		if (index < 0) throw new ArgumentOutOfRangeException(nameof(operation), $"Not an {nameof(Operation)} scheduled to this {nameof(Device)}.");

		using var _ = locker.Fetch();
		ThrowIfDisposed();

		if (!operations.Skip(index)) return;
		foreach (var worker in workers) worker.Abort();
	}

	public void Dispose()
	{
		using var _ = locker.Fetch();
		if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

		//Dispose workers
		foreach (var worker in workers) worker.Dispose();

		//Dispose operations
		operations.Dispose();
	}

	void OnDispatchChanged(Worker worker, bool entered)
	{
		if (entered)
		{
			uint count = Interlocked.Increment(ref dispatchPositives);
			Ensure.IsTrue(count <= Population);
		}
		else
		{
			uint count = Interlocked.Increment(ref dispatchNegatives);
			Ensure.IsTrue(count <= Volatile.Read(ref dispatchPositives));

			if (count != Population) return;

			Volatile.Write(ref dispatchPositives, 0);
			Volatile.Write(ref dispatchNegatives, 0);

			if (Disposed) return;

			using var _ = locker.Fetch();
			if (operations.Advance(out Operation next)) Dispatch(next);
		}
	}

	void OnIdlenessChanged(Worker worker, bool entered) => operations.Current.ChangeWorkerIdleness(worker, entered);

	void ThrowIfDisposed()
	{
		if (Disposed) throw new ObjectDisposedException(nameof(Device));
	}

	void Dispatch(Operation operation)
	{
		Ensure.AreEqual(Volatile.Read(ref dispatchPositives), 0u);
		Ensure.AreEqual(Volatile.Read(ref dispatchNegatives), 0u);
		Ensure.IsTrue(locker.IsEntered);

		ThrowIfDisposed();
		foreach (var worker in workers) worker.Dispatch(operation);
	}

	public interface IOperations : IDisposable
	{
		/// <summary>
		/// The total number of <see cref="Operation"/> that has ever been scheduled to this <see cref="Device"/>.
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// The index of the current executing <see cref="Operation"/> or if nothing
		/// is executing, the latest <see cref="Operation"/> that finished executing.
		/// </summary>
		public int LatestIndex { get; }

		/// <summary>
		/// Similar to using <see cref="LatestIndex"/> on the indexer <see cref="this"/>,
		/// except null is returned if <see cref="Count"/> is zero (rather than an exception).
		/// </summary>
		public sealed Operation Latest => Count == 0 ? null : this[LatestIndex];

		/// <summary>
		/// Retrieves an <see cref="Operation"/>. 
		/// </summary>
		/// <param name="index">The index to retrieve, must be greater than or equals zero and less than <see cref="Count"/>.</param>
		public Operation this[int index] { get; }

		/// <summary>
		/// Returns the index of an <see cref="Operation"/> scheduled to this <see cref="Device"/>.
		/// </summary>
		/// <param name="operation">The <see cref="Operation"/> to look for.</param>
		/// <returns>The index if found, otherwise a negative number.</returns>
		public int IndexOf(Operation operation);

		/// <summary>
		/// Blocks the calling thread until this <see cref="Device"/> completes or aborts <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The <see cref="Operation"/> to await for.</param>
		/// <exception cref="ArgumentOutOfRangeException">Argument is not an <see cref="Operation"/> scheduled to this <see cref="Device"/>.</exception>
		public void Await(Operation operation);
	}

	sealed class OperationsQueue : IOperations
	{
		/// <summary>
		/// If an operation is being worked on, the index of that operation, otherwise, <see cref="Count"/>.
		/// </summary>
		int currentIndex;

		readonly List<Operation> list = new();
		readonly HashSet<int> skipIndices = new();
		readonly ReaderWriterLockSlim locker = new();
		readonly Locker awaitLocker = new();

		/// <inheritdoc/>
		public int Count
		{
			get
			{
				locker.EnterReadLock();
				try { return list.Count; }
				finally { locker.ExitReadLock(); }
			}
		}

		/// <inheritdoc/>
		public int LatestIndex
		{
			get
			{
				locker.EnterReadLock();
				try
				{
					if (list.Count == 0) return 0;
					return Math.Min(currentIndex, list.Count - 1);
				}
				finally { locker.ExitReadLock(); }
			}
		}

		/// <summary>
		/// The <see cref="Operation"/> that is currently being worked on, or undefined behavior if nothing is being worked on.
		/// </summary>
		public Operation Current
		{
			get
			{
				locker.EnterReadLock();
				try { return list[currentIndex]; }
				finally { locker.ExitReadLock(); }
			}
		}

		/// <inheritdoc/>
		public Operation this[int index]
		{
			get
			{
				locker.EnterReadLock();
				try { return list[index]; }
				finally { locker.ExitReadLock(); }
			}
		}

		/// <returns>Whether this <see cref="Operation"/> that we just enqueued should be immediately dispatched.</returns>
		public bool Enqueue(Operation operation)
		{
			locker.EnterWriteLock();
			try
			{
				Ensure.IsFalse(list.Contains(operation));
				bool dispatch = list.Count == currentIndex;
				list.Add(operation);
				return dispatch;
			}
			finally { locker.ExitWriteLock(); }
		}

		/// <summary>
		/// Marks <see cref="Current"/> as done and continue to the next <see cref="Operation"/>.
		/// </summary>
		/// <param name="next">The next <see cref="Operation"/> if returns true, undefined otherwise.</param>
		/// <returns>Whether there are more <see cref="Operation"/> to be dispatched.</returns>
		public bool Advance(out Operation next)
		{
			locker.EnterWriteLock();
			try
			{
				int index;

				//Find next non-skipped index
				do index = ++currentIndex;
				while (skipIndices.Remove(index));

				if (index == list.Count)
				{
					next = default;
					return false;
				}

				//Get the operation from index and return true
				Ensure.IsTrue(currentIndex < list.Count);
				next = list[currentIndex];
				return true;
			}
			finally
			{
				locker.ExitWriteLock();
				awaitLocker.Signal(); //Signal for operation completion
			}
		}

		/// <inheritdoc/>
		public int IndexOf(Operation operation)
		{
			locker.EnterReadLock();
			try { return list.IndexOf(operation); }
			finally { locker.ExitReadLock(); }
		}

		/// <inheritdoc/>
		public void Await(Operation operation)
		{
			int index = IndexOf(operation);
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(operation), $"Not an {nameof(Operation)} scheduled to this {nameof(Device)}.");

			if (Done()) return;

			lock (awaitLocker)
			{
				do awaitLocker.Wait();
				while (!Done() && awaitLocker.Signaling);
			}

			bool Done()
			{
				locker.EnterReadLock();
				try { return index < currentIndex; }
				finally { locker.ExitReadLock(); }
			}
		}

		/// <summary>
		/// Skips the execution of an <see cref="Operation"/>.
		/// </summary>
		/// <param name="index">The index of the <see cref="Operation"/> to skip.</param>
		/// <returns>Whether the <see cref="Device"/> need to skip the current <see cref="Operation"/>.</returns>
		public bool Skip(int index)
		{
			Ensure.IsTrue(index >= 0);

			locker.EnterWriteLock();
			try
			{
				if (index < currentIndex) return false; //Operation already done
				if (index == currentIndex) return true;

				skipIndices.Add(index);
				return false;
			}
			finally
			{
				locker.ExitWriteLock();
			}
		}

		public void Dispose()
		{
			awaitLocker.Signaling = false;

			locker.EnterWriteLock();
			try
			{
				for (int i = 0; i < list.Count; i++) list[i].Dispose();

				list.Clear();
				skipIndices.Clear();
			}
			finally { locker.ExitWriteLock(); }

			locker.Dispose();
		}
	}
}