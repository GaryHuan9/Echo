using System.Threading;

namespace Echo.Core.Compute;

/// <summary>
/// A task with individual steps to be executed concurrently.
/// </summary>
public abstract class Operation
{
	ulong currentProcedure;

	/// <summary>
	/// Prepares this <see cref="Operation"/> for execution.
	/// </summary>
	/// <param name="population">The maximum number of concurrent <see cref="IScheduler"/> executing. Note that
	/// all <see cref="IScheduler.Id"/> will be between zero (inclusive) and this value (exclusive).</param>
	/// <remarks>This method is invoked once before all execution begins</remarks>
	public virtual void Prepare(int population)
	{
		Interlocked.Exchange(ref currentProcedure, 0);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="scheduler">The <see cref="IScheduler"/> used.</param>
	/// <returns>Whether this execution performed any work.</returns>
	public bool Execute(IScheduler scheduler)
	{
		ulong procedure = Interlocked.Increment(ref currentProcedure);
		return Execute(procedure - 1, scheduler);
	}

	/// <summary>
	/// Executes one step of this <see cref="Operation"/>.
	/// </summary>
	/// <param name="procedure">The number/index of the step to execute.</param>
	/// <param name="scheduler">The <see cref="IScheduler"/> used.</param>
	/// <returns>Whether this step performed any work.</returns>
	/// <remarks>Once this method returns false, most steps after this one will not be executed.
	/// However, it is not guaranteed that ALL later steps will not be executed.</remarks>
	protected abstract bool Execute(ulong procedure, IScheduler scheduler);
}