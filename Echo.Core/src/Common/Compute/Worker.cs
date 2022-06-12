using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Diagnostics;

namespace Echo.Core.Common.Compute;

/// <summary>
/// An external delegation representing the <see cref="Worker"/> that can execute an <see cref="Operation"/>.
/// </summary>
public interface IWorker
{
	/// <summary>
	/// Two <see cref="IWorker"/> with the same <see cref="Index"/> will never execute the
	/// same <see cref="Operation"/> at the same time. Additionally, the value of this property
	/// will start at zero and continues on for different <see cref="IWorker"/>.
	/// </summary>
	int Index { get; }

	/// <summary>
	/// A globally unique identifier (<see cref="Guid"/>) for this <see cref="IWorker"/>.
	/// </summary>
	/// <remarks>The uniqueness of this value transcends different <see cref="Device"/>, while the
	/// uniqueness of <see cref="Index"/> does not; they are used for different purposes.</remarks>
	Guid Guid { get; }

	/// <summary>
	/// The current <see cref="WorkerState"/> of this <see cref="IWorker"/>.
	/// </summary>
	WorkerState State { get; }

	/// <summary>
	/// The <see cref="string"/> label of this <see cref="IWorker"/> to be displayed.
	/// </summary>
	sealed string DisplayLabel => $"Worker {Guid:D}";

	/// <summary>
	/// Invoked when this <see cref="IWorker"/> either begins or stops being idle.
	/// </summary>
	/// <remarks>This is not invoked at the exact time when <see cref="State"/> changed, but rather when 
	/// the value is changed internally, thus it is more accurate but invoked on a different thread.</remarks>
	event Action<IWorker, bool> OnIdleChangedEvent;

	/// <summary>
	/// Invoked when this <see cref="IWorker"/> either begins or stops being awaiting for resumption (ie. paused).
	/// </summary>
	/// <remarks>This is not invoked at the exact time when <see cref="State"/> changed, but rather when 
	/// the value is changed internally, thus it is more accurate but invoked on a different thread.</remarks>
	event Action<IWorker, bool> OnAwaitChangedEvent;

	/// <summary>
	/// Checks if there are any schedule changes.
	/// </summary>
	/// <remarks>Should only be invoked periodically within the execution of an <see cref="Operation"/>.</remarks>
	void CheckSchedule();
}

/// <summary>
/// Different states of a <see cref="Worker"/>.
/// </summary>
/// <remarks>Almost always, the value of enum should be one-hot (eg. <see cref="Idle"/> and <see cref="Pausing"/>
/// should not be present at the same time). The <see cref="FlagsAttribute"/> is only there for the convenience of
/// some implementation details in <see cref="Worker.AwaitStatus"/>.</remarks>
[Flags]
public enum WorkerState : uint
{
	/// <summary>
	/// The <see cref="Worker"/> is has no assigned <see cref="Operation"/>.
	/// </summary>
	Idle = 1 << 0,

	/// <summary>
	/// The <see cref="Worker"/> is currently executing an <see cref="Operation"/>.
	/// </summary>
	Running = 1 << 1,

	/// <summary>
	/// The <see cref="Worker"/> is executing an <see cref="Operation"/> but will pause at its earliest convenience.
	/// </summary>
	Pausing = 1 << 2,

	/// <summary>
	/// The <see cref="Worker"/> is executing an <see cref="Operation"/> but will abort as soon as possible.
	/// </summary>
	Aborting = 1 << 3,

	/// <summary>
	/// The <see cref="Worker"/> is paused and is not utilizing any computational resources.
	/// </summary>
	Awaiting = 1 << 4,

	/// <summary>
	/// The <see cref="Worker"/> is disposed and should no longer be used.
	/// </summary>
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
	public Worker(int index)
	{
		Index = index;
		Guid = Guid.NewGuid();
	}

	Operation nextOperation;
	Thread thread;

	readonly Locker locker = new();

	/// <inheritdoc/>
	public int Index { get; }

	/// <inheritdoc/>
	public Guid Guid { get; }

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
		}
	}

	/// <inheritdoc/>
	public event Action<IWorker, bool> OnIdleChangedEvent;

	/// <inheritdoc/>
	public event Action<IWorker, bool> OnAwaitChangedEvent;

	/// <inheritdoc/>
	public int? ThreadId => thread?.ManagedThreadId;

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

		thread?.Join();
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

		OnAwaitChangedEvent?.Invoke(this, true);
		AwaitStatus(WorkerState.Running | WorkerState.Aborting);
		OnAwaitChangedEvent?.Invoke(this, false);

		CheckForAbortion();
	}

	void Main()
	{
		while (State != WorkerState.Disposed)
		{
			if (!AwaitStatus(WorkerState.Running)) break; //Disposed

			Operation operation = Interlocked.Exchange(ref nextOperation, null);
			if (operation == null) throw new InvalidAsynchronousStateException();

			OnIdleChangedEvent?.Invoke(this, false);
			bool running;

			do
			{
				try
				{
					running = operation.Execute(this);
					((IWorker)this).CheckSchedule();
				}
				catch (Operation.AbortException)
				{
					break;
				}
			}
			while (running);

			State = WorkerState.Idle;
			OnIdleChangedEvent?.Invoke(this, true);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool AwaitStatus(WorkerState status)
	{
		status |= WorkerState.Disposed;

		lock (locker)
		{
			WorkerState state = State;

			while ((state | status) != status)
			{
				locker.Wait();
				state = State;
			}

			return state != WorkerState.Disposed;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void CheckForAbortion()
	{
		if (State != WorkerState.Aborting) return;
		throw new Operation.AbortException();
	}
}
