using System;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;

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

	readonly DualLock locker = new();
	readonly Signaler signaler = new();

	int idleCount;

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
		for (int i = 0; i < threads.Length; i++) LaunchThread(i);
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

	void LaunchThread(int index)
	{
		ref Thread thread = ref threads[index];
		if (thread != null) return;

		thread = new Thread(Main)
		{
			IsBackground = true, Priority = ThreadPriority.AboveNormal,
			Name = $"{nameof(Device)} {guid} {nameof(Thread)} {index}"
		};

		thread.Start(index);
	}

	void Main(object id)
	{
		using var scheduler = new Scheduler((int)id);

		while (true)
		{
			AwaitStatus(State.Running);

			Operation operation = Operation;
			if (operation == null) continue;

			Interlocked.Increment(ref idleCount);

			bool completed;

			try
			{
				completed = !operation.Execute(scheduler);
			}
			catch (OperationAbortedException)
			{
				throw;
			}

			bool allIdle = Interlocked.Decrement(ref idleCount) == 0;

			if (completed)
			{
				AssertNotDisposed();
				CompleteOperation();
			}
			else if (allIdle)
			{
				using var _ = locker.FetchWriteLock();
				Assert.AreEqual(InterlockedHelper.Read(ref idleCount), 0);

				if (Status == State.Pausing) Status = State.Paused;
				if (Status == State.Abandoning) Status = State.Idle;
			}
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
			lock (signaler) Monitor.Wait(signaler);
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