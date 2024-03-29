﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Echo.Core.Common.Compute.Statistics;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Threading;

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

		totalProcedure = totalProcedureCount;
		workerData = new AlignedArray<WorkerData>(count);

		for (int i = 0; i < count; i++) workerData[i] = new WorkerData(workers[i].Guid);

		creationTime = DateTime.Now;
	}

	/// <summary>
	/// The <see cref="DateTime"/> when this <see cref="Operation"/> was created.
	/// </summary>
	public readonly DateTime creationTime;

	protected AlignedArray<WorkerData> workerData;

	protected uint nextProcedure;
	uint totalProcedure;
	uint completedCount;

	TimeSpan totalRecordedTime;

	readonly Locker procedureLocker = new();
	readonly Locker totalTimeLocker = new();

	static readonly Stopwatch stopwatch = Stopwatch.StartNew();

	/// <summary>
	/// The total number of steps in this <see cref="Operation"/>.
	/// </summary>
	/// <remarks>In some <see cref="Operation"/>, this value might increase, but it will never decrease!</remarks>
	public uint TotalProcedureCount => InterlockedHelper.Read(ref totalProcedure);

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
	public bool IsCompleted => CompletedProcedureCount == TotalProcedureCount;

	/// <summary>
	/// The number of <see cref="IWorker"/>s working on completing this <see cref="Operation"/>.
	/// </summary>
	public int WorkerCount => workerData.Length;

	/// <summary>
	/// The progress of this <see cref="Operation"/>.
	/// </summary>
	/// <remarks>This value is between zero and one (both inclusive).</remarks>
	public double Progress
	{
		get
		{
			using var _ = procedureLocker.Fetch();
			if (completedCount == TotalProcedureCount) return 1d; //Fully completed

			double progress = 0d;

			foreach (ref readonly WorkerData data in workerData.AsSpan()) progress += data.procedure.Progress;

			return (progress + completedCount) / TotalProcedureCount;
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
				//Add the current running time if worker is running
				if (data.IsRunning) result += time - data.timeStarted;
			}

			return result;
		}
	}

	/// <summary>
	/// The duration in realtime that this <see cref="Operation"/> has been worked on.
	/// </summary>
	/// <remarks>This is the time of the most active <see cref="IWorker"/> spent on this <see cref="Operation"/>.</remarks>
	public TimeSpan Time
	{
		get
		{
			TimeSpan time = stopwatch.Elapsed;
			Ensure.AreNotEqual(time, default);

			using var _ = totalTimeLocker.Fetch();
			TimeSpan maxTime = TimeSpan.MinValue;

			foreach (ref readonly WorkerData data in workerData.AsSpan())
			{
				TimeSpan workerTime = data.recordedTime;
				if (data.IsRunning) workerTime += time - data.timeStarted;
				if (workerTime > maxTime) maxTime = workerTime;
			}

			return maxTime;
		}
	}

	/// <summary>
	/// Whether <see cref="Dispose"/> is invoked on this <see cref="Operation"/>.
	/// </summary>
	/// <remarks>If this is true, the behavior of all members are undefined.</remarks>
	public bool Disposed => workerData == null;

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="worker">The <see cref="IWorker"/> to use.</param>
	/// <returns>Whether the <see cref="IWorker"/> should invoke this method one more time.</returns>
	public virtual bool Execute(IWorker worker)
	{
		uint index = Interlocked.Increment(ref nextProcedure) - 1;
		if (index >= TotalProcedureCount) return false;

		//Fetch data and execute
		ref WorkerData data = ref workerData[worker.Index];
		data.ThrowIfInconsistent(worker.Guid);
		data.procedure = new Procedure(index);
		Execute(ref data.procedure, worker);

		//Update progress
		return CompleteProcedure(ref data);
	}

	/// <summary>
	/// Should be invoked when a <see cref="IWorker"/> either begins or stops actively working on this <see cref="Operation"/>.
	/// </summary>
	/// <param name="worker">The <see cref="IWorker"/> that is changing its idle state.</param>
	/// <param name="idle">True if the <see cref="IWorker"/> is stopping its execution, false otherwise.</param>
	/// <remarks>This method should be invoked directly through <see cref="Worker.OnDispatchChangedEvent"/>
	/// and <see cref="Worker.OnIdlenessChangedEvent"/>, otherwise the behavior is undefined.</remarks>
	public void ChangeWorkerIdleness(IWorker worker, bool idle)
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

			Ensure.IsFalse(data.IsRunning);
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
			if (!data.IsRunning) fill.Add(data.recordedTime);
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
	/// Completes the current <see cref="Procedure"/> in a <see cref="WorkerData"/>.
	/// </summary>
	/// <returns>Whether there are more <see cref="Procedure"/> to work on.</returns>
	protected bool CompleteProcedure(ref WorkerData data)
	{
		using var _ = procedureLocker.Fetch();
		data.procedure = default;

		uint completed = ++completedCount;
		uint total = TotalProcedureCount;
		Ensure.IsTrue(completed <= total);
		return completed < total;
	}

	/// <summary>
	/// Increases <see cref="TotalProcedureCount"/> as more potential <see cref="Procedure"/> is discovered.
	/// </summary>
	protected void IncreaseTotalProcedure(uint amount)
	{
		uint result = Interlocked.Add(ref totalProcedure, amount);
		Ensure.IsTrue(result > result - amount); //Total should only grow
	}

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

	[StructLayout(LayoutKind.Sequential, Size = 64)]
	protected struct WorkerData
	{
		public WorkerData(Guid workerGuid)
		{
			this.workerGuid = workerGuid;

			recordedTime = TimeSpan.Zero;
			timeStarted = default;
			procedure = default;
			monoThread = new MonoThread();
		}

		public readonly Guid workerGuid;

		public TimeSpan recordedTime;
		public TimeSpan timeStarted;
		public Procedure procedure;

		MonoThread monoThread;

		public readonly bool IsRunning => timeStarted != default;

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

	/// <summary>
	/// The number of <see cref="EventRow"/> available from <see cref="FillEventRows"/>.
	/// </summary>
	public static int EventRowCount => default(T).Count;

	unsafe T Sum => default(T).Sum(statsArray.Pointer, WorkerCount);

	/// <summary>
	/// Gets one <see cref="EventRow"/> from the information <see cref="IStatistics{T}"/>.
	/// </summary>
	/// <param name="index">The location to get; must be non-negative and smaller than <see cref="EventRowCount"/>.</param>
	public EventRow GetEventRow(int index) => Sum[index];

	/// <summary>
	/// Fills information about the events occured in this <see cref="Operation{T}"/>.
	/// </summary>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/> to be filled with <see cref="EventRow"/>s.</param>
	/// <remarks>The maximum number of available items filled from this method is <see cref="EventRowCount"/>.</remarks>
	public void FillEventRows(ref SpanFill<EventRow> fill)
	{
		fill.ThrowIfNotEmpty();

		T sum = Sum;
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