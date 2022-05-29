using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Diagnostics;

namespace Echo.Common.Compute;

/// <summary>
/// An external delegation representing the <see cref="Worker"/> that is executing an <see cref="Operation"/>.
/// </summary>
public interface IWorker
{
	/// <summary>
	/// Two <see cref="IWorker"/> with the same <see cref="Id"/> will never execute the
	/// same <see cref="Operation"/> at the same time. Additionally, the value of this property
	/// will start at zero and continues on for different <see cref="IWorker"/>.
	/// </summary>
	uint Id { get; }

	/// <summary>
	/// The current <see cref="WorkerState"/> of this <see cref="IWorker"/>.
	/// </summary>
	WorkerState State { get; }

	/// <summary>
	/// The <see cref="string"/> label of this <see cref="IWorker"/> to be displayed.
	/// </summary>
	sealed string DisplayLabel => $"Worker 0x{Id:X2}";

	/// <summary>
	/// Checks if there are any schedule changes.
	/// </summary>
	/// <remarks>Should be invoked periodically during an <see cref="Operation"/>.</remarks>
	void CheckSchedule();
}

[Flags]
public enum WorkerState : uint
{
	Idle = 1 << 0,
	Running = 1 << 1,
	Pausing = 1 << 2,
	Aborting = 1 << 3,
	Awaiting = 1 << 4,
	Disposed = 1 << 5
}

public static class WorkerStateExtensions
{
	static readonly string[] workerStateLabels = Enum.GetNames<WorkerState>();

	/// <summary>
	/// Converts a <see cref="WorkerState"/> to be displayed.
	/// </summary>
	/// <param name="state">The <see cref="WorkerState"/> to be converted.</param>
	/// <returns>A display <see cref="string"/> representing the <see cref="WorkerState"/>.</returns>
	/// <remarks>The <paramref name="state"/> must have only one bit enabled, and it must be one of the named
	/// values of <see cref="WorkerState"/>, otherwise the behavior of this method is undefined!</remarks>
	public static string ToDisplayString(this WorkerState state)
	{
		uint integer = (uint)state;
		Assert.AreEqual(BitOperations.PopCount(integer), 1);
		int index = BitOperations.LeadingZeroCount(integer);
		return workerStateLabels[31 - index];
	}
}

/// <summary>
/// A <see cref="Thread"/> that works under a <see cref="Device"/> to jointly perform certain <see cref="Operation"/>s.
/// </summary>
sealed class Worker : IWorker, IDisposable
{
	public Worker(uint id) => Id = id;

	Operation nextOperation;
	Thread thread;

	public event Action<Worker> OnRunEvent;
	public event Action<Worker> OnIdleEvent;

	/// <inheritdoc/>
	public uint Id { get; }

	uint _state = (uint)WorkerState.Idle;

	/// <inheritdoc/>
	public WorkerState State
	{
		get => (WorkerState)_state;
		private set
		{
			using var _ = locker.Fetch();

			WorkerState old = State;
			if (old == value) return;
			uint integer = (uint)value;

			Assert.AreNotEqual(old, WorkerState.Disposed);
			Assert.AreEqual(BitOperations.PopCount(integer), 1);

			Interlocked.Exchange(ref _state, integer);
			locker.Signal();

			bool wasIdle = old == WorkerState.Idle;
			bool isIdle = value == WorkerState.Idle;
			if (wasIdle != isIdle) (isIdle ? OnIdleEvent : OnRunEvent)?.Invoke(this);
		}
	}

	readonly Locker locker = new();

	/// <summary>
	/// Begins running an <see cref="Operation"/> on this idle <see cref="Worker"/>.
	/// </summary>
	/// <param name="operation">The <see cref="Operation"/> to dispatch</param>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="State"/> is not <see cref="WorkerState.Idle"/>.</exception>
	public void Dispatch(Operation operation)
	{
		using var _ = locker.Fetch();

		lock (locker)
		{
			if (State != WorkerState.Idle) throw new InvalidOperationException();

			//Assign operation
			Volatile.Write(ref nextOperation, operation);
			State = WorkerState.Running;
		}

		//Launch thread if needed
		if (thread == null)
		{
			thread = new Thread(Main)
			{
				IsBackground = true,
				Priority = ThreadPriority.Normal,
				Name = ((IWorker)this).DisplayLabel
			};

			thread.Start();
		}
	}

	/// <summary>
	/// If possible, pauses the <see cref="Operation"/> this <see cref="Worker"/> is currently performing as soon as possible.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="State"/> is <see cref="WorkerState.Disposed"/>.</exception>
	public void Pause()
	{
		using var _ = locker.Fetch();

		switch (State)
		{
			case WorkerState.Running: break;
			case WorkerState.Idle:
			case WorkerState.Pausing:
			case WorkerState.Aborting:
			case WorkerState.Awaiting: return;
			case WorkerState.Disposed: throw new InvalidOperationException();
			default:                   throw new ArgumentOutOfRangeException();
		}

		State = WorkerState.Pausing;
	}

	/// <summary>
	/// If possible, resumes the <see cref="Operation"/> this <see cref="Worker"/> was performing prior to pausing.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="State"/> is <see cref="WorkerState.Disposed"/>.</exception>
	public void Resume()
	{
		using var _ = locker.Fetch();

		switch (State)
		{
			case WorkerState.Pausing:
			case WorkerState.Awaiting: break;
			case WorkerState.Idle:
			case WorkerState.Running:
			case WorkerState.Aborting: return;
			case WorkerState.Disposed: throw new InvalidOperationException();
			default:                   throw new ArgumentOutOfRangeException();
		}

		State = WorkerState.Running;
	}

	/// <summary>
	/// If necessary, aborts the <see cref="Operation"/> this <see cref="Worker"/> is currently performing as soon as possible.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="State"/> is <see cref="WorkerState.Disposed"/>.</exception>
	public void Abort()
	{
		using var _ = locker.Fetch();

		switch (State)
		{
			case WorkerState.Running:
			case WorkerState.Pausing:
			case WorkerState.Awaiting: break;
			case WorkerState.Idle:
			case WorkerState.Aborting: return;
			case WorkerState.Disposed: throw new InvalidOperationException();
			default:                   throw new ArgumentOutOfRangeException();
		}

		State = WorkerState.Aborting;
	}

	public void Dispose()
	{
		lock (locker)
		{
			if (State == WorkerState.Disposed) return;

			Abort();
			AwaitStatus(WorkerState.Idle);
			State = WorkerState.Disposed;

			locker.Signaling = false;
		}

		thread.Join();
	}

	/// <inheritdoc/>
	void IWorker.CheckSchedule()
	{
		CheckForAbortion();
		if (State != WorkerState.Pausing) return;

		lock (locker)
		{
			if (State != WorkerState.Pausing) return;
			State = WorkerState.Awaiting;
		}

		AwaitStatus(WorkerState.Running | WorkerState.Aborting);
		CheckForAbortion();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CheckForAbortion()
		{
			if (State != WorkerState.Aborting) return;
			throw new OperationAbortedException();
		}
	}

	void Main()
	{
		while (State != WorkerState.Disposed)
		{
			AwaitStatus(WorkerState.Running);

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

			State = WorkerState.Idle;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void AwaitStatus(WorkerState status)
	{
		status |= WorkerState.Disposed;

		lock (locker)
		{
			while ((State | status) != status) locker.Wait();
		}
	}
}