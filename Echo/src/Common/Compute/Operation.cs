using System;
using System.Threading;
using Echo.Common.Memory;

namespace Echo.Common.Compute;

/// <summary>
/// A task with individual steps to be executed both sequentially and concurrently.
/// </summary>
public abstract class Operation : IDisposable
{
	ulong nextProcedure;

	/// <summary>
	/// The number of steps that is either currently executing or has already been executed.
	/// </summary>
	public ulong StartedProcedureCount => Math.Min(TotalProcedureCount, nextProcedure);

	/// <summary>
	/// The total number of steps that this operation has.
	/// </summary>
	public ulong TotalProcedureCount { get; private set; }

	/// <summary>
	/// Prepares this <see cref="Operation"/> for execution.
	/// </summary>
	/// <param name="population">The maximum number of concurrent <see cref="IScheduler"/> executing. Note that
	/// all <see cref="IScheduler.Id"/> will be between zero (inclusive) and this value (exclusive).</param>
	/// <remarks>This method is invoked once before all execution begins</remarks>
	public virtual void Prepare(int population)
	{
		Interlocked.Exchange(ref nextProcedure, 0);
		TotalProcedureCount = WarmUp(population);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="scheduler">The <see cref="IScheduler"/> to use.</param>
	/// <returns>Whether this execution performed any work.</returns>
	public bool Execute(IScheduler scheduler)
	{
		ulong procedure = Interlocked.Increment(ref nextProcedure) - 1;
		if (procedure >= TotalProcedureCount) return false;

		Execute(procedure, scheduler);
		return true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc cref="Prepare"/>
	/// <returns>The number of steps the entire <see cref="Operation"/> has.</returns>
	protected abstract ulong WarmUp(int population);

	/// <summary>
	/// Executes one step of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="procedure">The number/index of the step to execute; this value is between 0 (inclusive)
	/// and the returned number from <see cref="WarmUp"/> (exclusive). This value will gradually increase as
	/// this <see cref="Operation"/> gets completed.</param>
	/// <param name="scheduler">The to <see cref="IScheduler"/> use.</param>
	protected abstract void Execute(ulong procedure, IScheduler scheduler);

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

	/// <summary>
	/// Retrieves the current statistics of this <see cref="Operation"/>.
	/// </summary>
	public unsafe T Statistics => default(T).Sum(array.Pointer, statisticsCount);

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

	protected sealed override void Execute(ulong procedure, IScheduler scheduler)
	{
		ref T statistics = ref array[(int)procedure];
		Execute(procedure, scheduler, ref statistics);
	}

	/// <inheritdoc cref="Execute(ulong, Echo.Common.Compute.IScheduler)"/>
	/// <param name="statistics">The <see cref="IStatistics{T}"/> to be used with this execution.</param>
	// ReSharper disable InvalidXmlDocComment
	protected abstract void Execute(ulong procedure, IScheduler scheduler, ref T statistics);
	// ReSharper restore InvalidXmlDocComment

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) array?.Dispose();
	}
}