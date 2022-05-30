using System;
using System.Threading;
using CodeHelpers.Diagnostics;
using Echo.Common.Compute.Statistics;
using Echo.Common.Memory;

namespace Echo.Common.Compute;

/// <summary>
/// A task with individual steps to be executed both sequentially and concurrently.
/// </summary>
public abstract class Operation : IDisposable
{
	uint nextProcedure;
	uint completedCount;

	/// <summary>
	/// The total number of steps that this operation has.
	/// </summary>
	public uint TotalProcedureCount { get; private set; }

	/// <summary>
	/// The number of steps in this operation that has been completed.
	/// </summary>
	public uint CompletedProcedureCount => completedCount;

	/// <summary>
	/// Returns whether this operation has been fully completed.
	/// </summary>
	public bool IsCompleted => completedCount == TotalProcedureCount;

	/// <summary>
	/// The number of <see cref="EventRow"/> available from <see cref="FillEventRows"/>.
	/// </summary>
	public virtual int EventCount => 0;

	/// <summary>
	/// Prepares this <see cref="Operation"/> for execution.
	/// </summary>
	/// <param name="population">The maximum number of concurrent <see cref="IWorker"/> executing. Note that
	/// all <see cref="IWorker.Id"/> will be between zero (inclusive) and this value (exclusive).</param>
	/// <remarks>This method is invoked once before all execution begins</remarks>
	public virtual void Prepare(int population)
	{
		Interlocked.Exchange(ref nextProcedure, 0);
		Interlocked.Exchange(ref completedCount, 0);
		TotalProcedureCount = WarmUp(population);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="worker">The <see cref="IWorker"/> to use.</param>
	/// <returns>Whether this execution performed any work.</returns>
	public bool Execute(IProcedureWorker worker)
	{
		uint procedure = Interlocked.Increment(ref nextProcedure) - 1;
		if (procedure >= TotalProcedureCount) return false;

		Execute(procedure, worker);
		Interlocked.Increment(ref completedCount);

		return true;
	}

	/// <summary>
	/// Fills information about the events occured in this <see cref="Operation"/>.
	/// </summary>
	/// <param name="fill">The destination to fill with <see cref="EventRow"/> elements.</param>
	/// <remarks>The maximum number of available <see cref="EventRow"/> can be determined by the
	/// value of <see cref="EventCount"/>.</remarks>
	public virtual void FillEventRows(ref SpanFill<EventRow> fill) => Assert.AreEqual(EventCount, 0);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc cref="Prepare"/>
	/// <returns>The number of steps the entire <see cref="Operation"/> has.</returns>
	protected abstract uint WarmUp(int population);

	/// <summary>
	/// Executes one step of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="procedure">The number/index of the step to execute; this value is between 0 (inclusive)
	///     and the returned number from <see cref="WarmUp"/> (exclusive). This value will gradually increase as
	///     this <see cref="Operation"/> gets completed.</param>
	/// <param name="worker">The to <see cref="IWorker"/> use.</param>
	protected abstract void Execute(uint procedure, IProcedureWorker worker);

	/// <summary>
	/// Releases the resources owned by this <see cref="Operation"/>.
	/// </summary>
	/// <param name="disposing">If true, this is invoked by the <see cref="IDisposable.Dispose"/> method, otherwise it is invoked by the finalizer.</param>
	protected virtual void Dispose(bool disposing) { }
}

/// <summary>
/// An <see cref="Operation"/> that supports event recording through <see cref="IStatistics{T}"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="IStatistics{T}"/> to use.</typeparam>
public abstract class Operation<T> : Operation where T : unmanaged, IStatistics<T>
{
	AlignedArray<T> array;
	int statisticsCount;

	public sealed override int EventCount => default(T).Count;

	public override void Prepare(int population)
	{
		base.Prepare(population);

		if (array == null || array.Length < population)
		{
			array?.Dispose();
			array = new AlignedArray<T>(population);
		}
		else array.Clear();

		statisticsCount = population;
	}

	public override unsafe void FillEventRows(ref SpanFill<EventRow> fill)
	{
		fill.ThrowIfNotEmpty();

		T sum = default(T).Sum(array.Pointer, statisticsCount);
		int length = Math.Min(fill.Length, EventCount);
		for (int i = 0; i < length; i++) fill.Add(sum[i]);
	}

	protected sealed override void Execute(uint procedure, IProcedureWorker worker)
	{
		ref T statistics = ref array[(int)worker.Id];
		Execute(procedure, worker, ref statistics);
	}

	/// <inheritdoc cref="Execute(uint,Echo.Common.Compute.IProcedureWorker)"/>
	/// <param name="statistics">The <see cref="IStatistics{T}"/> to be used with this execution.</param>
	// ReSharper disable InvalidXmlDocComment
	protected abstract void Execute(uint procedure, IProcedureWorker worker, ref T statistics);
	// ReSharper restore InvalidXmlDocComment

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) array?.Dispose();
	}
}

/// <summary>
/// Exception thrown by <see cref="IWorker"/> during an <see cref="Operation"/> abortion.
/// </summary>
sealed class OperationAbortedException : Exception { }