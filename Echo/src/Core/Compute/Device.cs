using System;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;

namespace Echo.Core.Compute;

public sealed partial class Device : IDisposable
{
	public Device() : this(Environment.ProcessorCount) { }

	public Device(int population)
	{
		guid = Guid.NewGuid();
		threads = new Thread[population];
	}

	readonly Guid guid;
	readonly Thread[] threads;

	readonly DualLock locker = new();
	readonly Signaler signaler = new();

	int runningCount;

	public int Population => threads.Length;

	State _status;
	Operation _operation;

	public State Status
	{
		get
		{
			using (locker.FetchReadLock()) return _status;
		}
		private set
		{
			using (locker.FetchWriteLock())
			{
				if (_status == value) return;
				AssertNotDisposed();
				_status = value;
			}

			signaler.Signal();
		}
	}

	public Operation Operation
	{
		get
		{
			using (locker.FetchReadLock()) return _status is State.Idle or State.Disposed ? null : _operation;
		}
		private set
		{
			using (locker.FetchWriteLock()) _operation = value;
		}
	}

	public void Dispatch(Operation operation)
	{
		operation.Prepare(Population);

		//Abandon previous operation if needed
		using var _0 = locker.FetchUpgradeableReadLock();

		Abandon();
		AwaitIdle();

		//Assign operation
		using var _1 = locker.FetchWriteLock();

		Operation = operation;
		Status = State.Running;

		//Launch threads
		for (uint i = 0; i < threads.Length; i++)
		{
			ref Thread thread = ref threads[i];
			if (thread != null) continue;

			thread = new Thread(Main)
			{
				IsBackground = true, Priority = ThreadPriority.Normal,
				Name = $"{nameof(Device)} {guid} {nameof(Thread)} {i}"
			};

			thread.Start(i);
		}
	}

	public void Pause() { }

	public void Resume() { }

	public void Abandon()
	{
		if (Status == State.Idle) return;

		using var _ = locker.FetchWriteLock();

		if (Status == State.Idle) return;

		Status = State.Abandoning;
	}

	public void Abort() { }

	public void AwaitIdle()
	{
		AwaitStatus(State.Idle);

		Assert.IsNull(Operation);
		Assert.IsTrue(Status == State.Idle);
	}

	public void Dispose()
	{
		using var _0 = locker.FetchUpgradeableReadLock();

		Abort();
		AwaitIdle();

		using var _1 = locker.FetchWriteLock();

		//TODO: dispose
	}

	void Main(object id)
	{
		using var scheduler = new Scheduler((uint)id);

		while (true)
		{
			AwaitStatus(State.Running);

			Operation operation = Operation;
			if (operation == null) continue;

			bool running;

			Interlocked.Increment(ref runningCount);

			do
			{
				try
				{
					running = operation.Execute(scheduler);
				}
				catch (OperationAbortedException)
				{
					throw;
				}
			}
			while (running);

			if (Interlocked.Decrement(ref runningCount) == 0)
			{
				CompleteOperation();
			}
			else AwaitStatus(State.Idle);

			// if (completed)
			// {
			// 	AssertNotDisposed();
			// 	CompleteOperation();
			// }
			// else if (allIdle)
			// {
			// 	using var _ = locker.FetchWriteLock();
			// 	Assert.AreEqual(InterlockedHelper.Read(ref runningCount), 0);
			//
			// 	if (Status == State.Pausing) Status = State.Paused;
			// 	if (Status == State.Abandoning) Status = State.Idle;
			// }
		}
	}

	void CompleteOperation()
	{
		if (HasNoOperation()) return;

		using var _ = locker.FetchWriteLock();

		if (HasNoOperation()) return;

		Status = State.Idle;

		bool HasNoOperation() => Status is not State.Running and not State.Pausing and not State.Abandoning and not State.Paused;
	}

	void AwaitStatus(State status)
	{
		while (true)
		{
			State current = Status;

			if (current == status || current == State.Disposed) return;
			signaler.Wait();
		}
	}

	void AssertNotDisposed() => Assert.AreNotEqual(Status, State.Disposed);

	public enum State
	{
		Idle,
		Running,
		Pausing,
		Abandoning,
		Paused,
		Disposed
	}
}