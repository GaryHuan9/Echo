using System.Threading;

namespace Echo.Core.Compute;

/// <summary>
/// A task with individual steps to be executed both sequentially and concurrently.
/// </summary>
public abstract class Operation
{
	ulong nextProcedure;
	ulong procedureCount;

	/// <summary>
	/// Prepares this <see cref="Operation"/> for execution.
	/// </summary>
	/// <param name="population">The maximum number of concurrent <see cref="IScheduler"/> executing. Note that
	/// all <see cref="IScheduler.Id"/> will be between zero (inclusive) and this value (exclusive).</param>
	/// <remarks>This method is invoked once before all execution begins</remarks>
	public void Prepare(int population)
	{
		Interlocked.Exchange(ref nextProcedure, unchecked((ulong)0 - 1));
		procedureCount = WarmUp(population);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="scheduler">The <see cref="IScheduler"/> to use.</param>
	/// <returns>Whether this execution performed any work.</returns>
	public bool Execute(IScheduler scheduler)
	{
		ulong procedure = Interlocked.Increment(ref nextProcedure);
		if (procedure >= procedureCount) return false;

		Execute(procedure, scheduler);
		return true;
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
}