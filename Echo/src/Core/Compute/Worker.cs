using System;
using System.Numerics;
using System.Threading;
using CodeHelpers.Diagnostics;
using Echo.Common;

namespace Echo.Core.Compute;

/// <summary>
/// A <see cref="Thread"/> that works under a <see cref="Device"/> to jointly perform certain <see cref="Operation"/>s.
/// </summary>
public sealed class Worker : IScheduler, IDisposable
{
	public Worker(uint id) => Id = id;

	Operation nextOperation;
	Thread thread;

	public event Action<Worker> OnRunEvent;
	public event Action<Worker> OnIdleEvent;

	/// <inheritdoc/>
	public uint Id { get; }

	uint _status = (uint)State.Idle;

	/// <summary>
	/// Accesses the current <see cref="State"/> of this <see cref="Worker"/>.
	/// </summary>
	public State Status
	{
		get => (State)_status;
		private set
		{
			using var _ = locker.Fetch();

			State old = Status;
			if (old == value) return;
			uint integer = (uint)value;

			Assert.AreNotEqual(old, State.Disposed);
			Assert.AreEqual(BitOperations.PopCount(integer), 1);

			Interlocked.Exchange(ref _status, integer);
			locker.Signal();

			bool wasIdle = old == State.Idle;
			bool isIdle = value == State.Idle;
			if (wasIdle != isIdle) (isIdle ? OnIdleEvent : OnRunEvent)?.Invoke(this);
		}
	}

	readonly Locker locker = new();

	/// <summary>
	/// Begins running an <see cref="Operation"/> on this idle <see cref="Worker"/>.
	/// </summary>
	/// <param name="operation">The <see cref="Operation"/> to dispatch</param>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is not <see cref="State.Idle"/>.</exception>
	public void Dispatch(Operation operation)
	{
		using var _ = locker.Fetch();

		lock (locker)
		{
			if (Status != State.Idle) throw new InvalidOperationException();

			//Assign operation
			Volatile.Write(ref nextOperation, operation);
			Status = State.Running;
		}

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
	}

	/// <summary>
	/// If possible, pauses the <see cref="Operation"/> this <see cref="Worker"/> is currently performing as soon as possible.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is <see cref="State.Disposed"/>.</exception>
	public void Pause()
	{
		using var _ = locker.Fetch();

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

	/// <summary>
	/// If possible, resumes the <see cref="Operation"/> this <see cref="Worker"/> was performing prior to pausing.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is <see cref="State.Disposed"/>.</exception>
	public void Resume()
	{
		using var _ = locker.Fetch();

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

	/// <summary>
	/// If necessary, aborts the <see cref="Operation"/> this <see cref="Worker"/> is currently performing as soon as possible.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is <see cref="State.Disposed"/>.</exception>
	public void Abort()
	{
		using var _ = locker.Fetch();

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

	public void Dispose()
	{
		lock (locker)
		{
			if (Status == State.Disposed) return;

			Abort();
			AwaitStatus(State.Idle);
			Status = State.Disposed;

			locker.Signaling = false;
		}

		thread.Join();
	}

	/// <inheritdoc/>
	void IScheduler.CheckSchedule()
	{
		CheckForAbortion();
		if (Status != State.Pausing) return;

		lock (locker)
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
		while (Status != State.Disposed)
		{
			AwaitStatus(State.Running);

			var operation = Interlocked.Exchange(ref nextOperation, null);
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

			Status = State.Idle;
		}
	}

	void AwaitStatus(State status)
	{
		status |= State.Disposed;

		lock (locker)
		{
			while ((Status | status) != status) locker.Wait();
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