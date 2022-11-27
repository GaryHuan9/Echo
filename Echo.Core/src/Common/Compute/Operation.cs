using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Echo.Core.Common.Compute.Statistics;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;

namespace Echo.Core.Common.Compute;

/// <summary>
/// A task with individual steps to be executed both sequentially and concurrently.
/// </summary>
public abstract class Operation : IDisposable
{
	/// <summary>
	/// Constructs a new <see cref="Operation"/>.
	/// </summary>
	/// <param name="workers">All the <see cref="IWorker"/>s that will be working on this <see cref="Operation"/>.</param>
	/// <param name="totalProcedureCount">The total number of steps this entire <see cref="Operation"/> has.</param>
	protected Operation(ImmutableArray<IWorker> workers, uint totalProcedureCount)
	{
		int count = workers.Length;

		this.totalProcedureCount = totalProcedureCount;
		workerData = new AlignedArray<WorkerData>(count);

		for (int i = 0; i < count; i++) workerData[i] = new WorkerData(workers[i].Guid);

		creationTime = DateTime.Now;
	}

	/// <summary>
	/// The total number of steps in this <see cref="Operation"/>.
	/// </summary>
	public readonly uint totalProcedureCount;

	/// <summary>
	/// The <see cref="DateTime"/> when this <see cref="Operation"/> was created.
	/// </summary>
	public readonly DateTime creationTime;

	AlignedArray<WorkerData> workerData;

	uint nextProcedure;
	uint completedCount;

	TimeSpan totalRecordedTime;

	readonly Locker procedureLocker = new();
	readonly Locker totalTimeLocker = new();

	static readonly Stopwatch stopwatch = Stopwatch.StartNew();

	/// <summary>
	/// The number of steps already completed.
	/// </summary>
	/// <remarks>This does not mean all steps with <see cref="Procedure.index"/> from 0 (inclusive) to this
	/// number (exclusive) are completed, because some steps with <see cref="Procedure.index"/> lower than
	/// this number can still be worked on while other steps with <see cref="Procedure.index"/> higher than
	/// this number might have already been completed.</remarks>
	public uint CompletedProcedureCount
	{
		get
		{
			lock (procedureLocker) return completedCount;
		}
	}

	/// <summary>
	/// Whether this operation has been fully completed.
	/// </summary>
	public bool IsCompleted => CompletedProcedureCount == totalProcedureCount;

	/// <summary>
	/// The number of <see cref="IWorker"/>s working on completing this <see cref="Operation"/>.
	/// </summary>
	public int WorkerCount => workerData.Length;

	/// <summary>
	/// The number of <see cref="EventRow"/> available from <see cref="FillEventRows"/>.
	/// </summary>
	public virtual int EventRowCount => 0;

	/// <summary>
	/// The progress of this <see cref="Operation"/>.
	/// </summary>
	/// <remarks>This value is between zero and one (both inclusive).</remarks>
	public double Progress
	{
		get
		{
			using var _ = procedureLocker.Fetch();
			if (completedCount == totalProcedureCount) return 1d; //Fully completed

			double progress = 0d;

			foreach (ref readonly WorkerData data in workerData.AsSpan()) progress += data.procedure.Progress;

			return (progress + completedCount) / totalProcedureCount;
		}
	}

	/// <summary>
	/// The total time this <see cref="Operation"/> has been worked by all of its <see cref="IWorker"/>s.
	/// </summary>
	/// <remarks>This value is scaled by the <see cref="WorkerCount"/> (eg. this value will 
	/// be 6 seconds if a <see cref="WorkerCount"/> of 3 worked for 2 seconds each).</remarks>
	public TimeSpan TotalTime
	{
		get
		{
			TimeSpan time = stopwatch.Elapsed;
			Ensure.AreNotEqual(time, default);

			using var _ = totalTimeLocker.Fetch();
			TimeSpan result = totalRecordedTime;

			foreach (ref readonly WorkerData data in workerData.AsSpan())
			{
				//A start time of default means that the worker is not running
				if (data.timeStarted != default) result += time - data.timeStarted;
			}

			return result;
		}
	}

	/// <summary>
	/// The duration in realtime that this <see cref="Operation"/> has been worked on.
	/// </summary>
	/// <remarks>This is equals to <see cref="TotalTime"/> divided by <see cref="WorkerCount"/>.</remarks>
	public TimeSpan Time => TotalTime / WorkerCount;

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="worker">The <see cref="IWorker"/> to use.</param>
	/// <returns>Whether this execution performed any work.</returns>
	public bool Execute(IWorker worker)
	{
		uint index = Interlocked.Increment(ref nextProcedure) - 1;
		if (index >= totalProcedureCount) return false;

		//Fetch data and execute
		ref WorkerData data = ref workerData[worker.Index];
		ref Procedure procedure = ref data.procedure;
		data.ThrowIfInconsistent(worker.Guid);

		procedure = new Procedure(index);
		Execute(ref procedure, worker);

		//Update progress
		lock (procedureLocker)
		{
			++completedCount;
			procedure = default;
		}

		return true;
	}

	/// <summary>
	/// Should be invoked when a <see cref="IWorker"/> is either starting or stopping to execute this <see cref="Operation"/>.
	/// </summary>
	/// <param name="worker">The <see cref="IWorker"/> that is changing its idle state.</param>
	/// <param name="idle">True if the <see cref="IWorker"/> is stopping its execution, false otherwise.</param>
	/// <remarks>This method should be invoked directly through <see cref="Worker.OnIdleChangedEvent"/>
	/// and <see cref="Worker.OnAwaitChangedEvent"/>, otherwise the behavior is undefined.</remarks>
	public void ChangeWorkerState(IWorker worker, bool idle)
	{
		TimeSpan time = stopwatch.Elapsed;
		Ensure.AreNotEqual(time, default);

		ref WorkerData data = ref workerData[worker.Index];
		data.ThrowIfInconsistent(worker.Guid);

		if (idle)
		{
			//Stop timer
			using var _ = totalTimeLocker.Fetch();

			TimeSpan elapsed = time - data.timeStarted;

			totalRecordedTime += elapsed;
			data.recordedTime += elapsed;
			data.timeStarted = default;
		}
		else
		{
			//Start timer
			lock (totalTimeLocker) data.timeStarted = time;
		}
	}

	/// <summary>
	/// Fills the <see cref="Guid"/> of the <see cref="IWorker"/>s of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> to be filled with <see cref="Guid"/>s.</param>
	/// <remarks>The maximum number of available items filled from this method is <see cref="WorkerCount"/>.</remarks>
	public void FillWorkerGuids(ref SpanFill<Guid> fill)
	{
		fill.ThrowIfNotEmpty();
		int length = Math.Min(fill.Length, WorkerCount);
		for (int i = 0; i < length; i++) fill.Add(workerData[i].workerGuid);
	}

	/// <summary>
	/// Fills the time the <see cref="IWorker"/>s of this <see cref="Operation"/> spent on completing this <see cref="Operation"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> to be filled with <see cref="TimeSpan"/>s.</param>
	/// <remarks>The maximum number of available items filled from this method is <see cref="WorkerCount"/>.</remarks>
	public void FillWorkerTimes(ref SpanFill<TimeSpan> fill)
	{
		TimeSpan time = stopwatch.Elapsed;
		Ensure.AreNotEqual(time, default);

		fill.ThrowIfNotEmpty();

		int length = Math.Min(fill.Length, WorkerCount);

		using var _ = totalTimeLocker.Fetch();

		for (int i = 0; i < length; i++)
		{
			ref readonly WorkerData data = ref workerData[i];

			if (data.timeStarted == default) fill.Add(data.recordedTime);
			else fill.Add(data.recordedTime + time - data.timeStarted);
		}
	}

	/// <summary>
	/// Fills the current <see cref="Procedure"/> of each of the <see cref="IWorker"/>s of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> to be filled with <see cref="Procedure"/>s.</param>
	/// <remarks>The maximum number of available items filled from this method is <see cref="WorkerCount"/>.</remarks>
	public void FillWorkerProcedures(ref SpanFill<Procedure> fill)
	{
		fill.ThrowIfNotEmpty();
		int length = Math.Min(fill.Length, WorkerCount);

		using var _ = procedureLocker.Fetch();
		for (int i = 0; i < length; i++) fill.Add(workerData[i].procedure);
	}

	/// <summary>
	/// Fills information about the events occured in this <see cref="Operation"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> to be filled with <see cref="EventRow"/>s.</param>
	/// <remarks>The maximum number of available items filled from this method is <see cref="EventRowCount"/>.</remarks>
	public virtual void FillEventRows(ref SpanFill<EventRow> fill) => Ensure.AreEqual(EventRowCount, 0);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Executes one step of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="procedure">The step to execute.</param>
	/// <param name="worker">The <see cref="IWorker"/> that is executing this step.</param>
	protected abstract void Execute(ref Procedure procedure, IWorker worker);

	/// <summary>
	/// Releases the resources owned by this <see cref="Operation"/>.
	/// </summary>
	/// <param name="disposing">If true, this is invoked by the <see cref="IDisposable.Dispose"/> method, otherwise it is invoked by the finalizer.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing) return;

		workerData?.Dispose();
		workerData = null;
	}

	/// <summary>
	/// Exception thrown by <see cref="IWorker"/> during an <see cref="Operation"/> abortion.
	/// </summary>
	internal sealed class AbortException : Exception { }

	[StructLayout(LayoutKind.Sequential, Size = 64)]
	struct WorkerData
	{
		public WorkerData(Guid workerGuid)
		{
			this.workerGuid = workerGuid;

			recordedTime = TimeSpan.Zero;
			timeStarted = TimeSpan.Zero;
			procedure = default;
			monoThread = new MonoThread();
		}

		public readonly Guid workerGuid;

		public TimeSpan recordedTime;
		public TimeSpan timeStarted;
		public Procedure procedure;

		MonoThread monoThread;

		//The size of this struct should not be larger than 64 bytes
		//If we really need more room, expand the total to 128 bytes

		public void ThrowIfInconsistent(in Guid guid)
		{
			monoThread.Ensure();
			Ensure.AreEqual(workerGuid, guid);
		}
	}
}

/// <summary>
/// An <see cref="Operation"/> that supports event recording through <see cref="IStatistics{T}"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="IStatistics{T}"/> to use.</typeparam>
public abstract class Operation<T> : Operation where T : unmanaged, IStatistics<T>
{
	protected Operation(ImmutableArray<IWorker> workers, uint totalProcedureCount)
		: base(workers, totalProcedureCount) => statsArray = new AlignedArray<T>(workers.Length);

	readonly AlignedArray<T> statsArray;

	public sealed override int EventRowCount => default(T).Count;

	public override unsafe void FillEventRows(ref SpanFill<EventRow> fill)
	{
		fill.ThrowIfNotEmpty();

		T sum = default(T).Sum(statsArray.Pointer, WorkerCount);
		int length = Math.Min(fill.Length, EventRowCount);
		for (int i = 0; i < length; i++) fill.Add(sum[i]);
	}

	protected sealed override void Execute(ref Procedure procedure, IWorker worker)
	{
		ref T statistics = ref statsArray[worker.Index];
		Execute(ref procedure, worker, ref statistics);
	}

	/// <inheritdoc cref="Execute(ref Procedure, IWorker)"/>
	/// <param name="statistics">The <see cref="IStatistics{T}"/> to be used with this execution.</param>
	// ReSharper disable InvalidXmlDocComment
	protected abstract void Execute(ref Procedure procedure, IWorker worker, ref T statistics);
	// ReSharper restore InvalidXmlDocComment

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) statsArray?.Dispose();
	}
}