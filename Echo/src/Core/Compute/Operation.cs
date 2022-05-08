using System.Threading;

namespace Echo.Core.Compute;

public abstract class Operation
{
	ulong currentProcedure;

	public virtual void Prepare(int population)
	{
		Interlocked.Exchange(ref currentProcedure, 0);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="scheduler">The <see cref="IScheduler"/> used.</param>
	/// <returns>Whether this execution performed no work.</returns>
	public bool Execute(IScheduler scheduler)
	{
		ulong procedure = Interlocked.Increment(ref currentProcedure);
		return Execute(procedure - 1, scheduler);
	}

	protected abstract bool Execute(ulong procedure, IScheduler scheduler);
}