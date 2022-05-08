using System;
using System.Numerics;
using System.Threading;
using CodeHelpers.Diagnostics;
using Echo.Common;

namespace Echo.Core.Compute;

public class Worker : IScheduler
{
	public Worker(uint id)
	{
		Id = id;
	}

	Operation nextOperation;
	Thread thread;

	public event Action<Worker> OnRunEvent;
	public event Action<Worker> OnIdleEvent;

	/// <inheritdoc/>
	public uint Id { get; }

	uint _status = (uint)State.Idle;

	public State Status
	{
		get => (State)Volatile.Read(ref _status);
		private set
		{
			using var _ = manageLocker.Fetch();

			State old = Status;
			if (old == value) return;
			uint integer = (uint)value;

			Assert.AreNotEqual(old, State.Disposed);
			Assert.AreEqual(BitOperations.PopCount(integer), 1);

			Volatile.Write(ref _status, integer);
			signalLocker.Signal();

			bool wasIdle = old == State.Idle;
			bool isIdle = value == State.Idle;
			if (wasIdle != isIdle) (isIdle ? OnIdleEvent : OnRunEvent)?.Invoke(this);
		}
	}

	readonly Locker manageLocker = new();
	readonly Locker signalLocker = new();

	public void Dispatch(Operation operation)
	{
		lock (manageLocker)
		{
			if (Status != State.Idle) throw new InvalidOperationException();

			//Launch thread if needed
			if (thread == null)
			{
				thread = new Thread(Main)
				{
					IsBackground = true, Priority = ThreadPriority.Normal,
					Name = $"{nameof(Worker)} Thread {Guid.NewGuid():N}"
				};

				thread.Start();
			}

			//Assign operation
			Volatile.Write(ref nextOperation, operation);
			Status = State.Running;
		}
	}

	public void Pause()
	{
		lock (manageLocker)
		{
			switch (Status)
			{
				case State.Running: break;
				case State.Idle:
				case State.Pausing:
				case State.Aborting:
				case State.Awaiting: return;
				case State.Disposed: throw new InvalidOperationException();
				default:             throw new ArgumentOutOfRangeException();
			}

			Status = State.Pausing;
		}
	}

	public void Resume()
	{
		lock (manageLocker)
		{
			switch (Status)
			{
				case State.Pausing:
				case State.Awaiting: break;
				case State.Idle:
				case State.Running:
				case State.Aborting: return;
				case State.Disposed: throw new InvalidOperationException();
				default:             throw new ArgumentOutOfRangeException();
			}

			Status = State.Running;
		}
	}

	public void Abort()
	{
		lock (manageLocker)
		{
			switch (Status)
			{
				case State.Running:
				case State.Pausing:
				case State.Awaiting: break;
				case State.Idle:
				case State.Aborting: return;
				case State.Disposed: throw new InvalidOperationException();
				default:             throw new ArgumentOutOfRangeException();
			}

			Status = State.Aborting;
		}
	}

	/// <inheritdoc/>
	void IScheduler.CheckSchedule()
	{
		CheckForAbortion();
		if (Status != State.Pausing) return;

		lock (manageLocker)
		{
			if (Status != State.Pausing) return;
			Status = State.Awaiting;
		}

		AwaitStatus(State.Running | State.Aborting);
		CheckForAbortion();

		void CheckForAbortion()
		{
			if (Status != State.Aborting) return;
			throw new OperationAbortedException();
		}
	}

	void Main()
	{
		while (true)
		{
			AwaitStatus(State.Running);

			var operation = Volatile.Read(ref nextOperation);
			if (operation == null) continue;

			bool running;

			do
			{
				try
				{
					running = operation.Execute(this);
				}
				catch (OperationAbortedException)
				{
					break;
				}
			}
			while (running);

			lock (manageLocker) Status = State.Idle;
		}
	}

	void AwaitStatus(State status)
	{
		while ((Status | status) != status)
		{
			lock (signalLocker) Monitor.Wait(signalLocker);
		}
	}

	[Flags]
	public enum State : uint
	{
		Idle = 1 << 0,
		Running = 1 << 1,
		Pausing = 1 << 2,
		Aborting = 1 << 3,
		Awaiting = 1 << 4,
		Disposed = 1 << 5
	}
}