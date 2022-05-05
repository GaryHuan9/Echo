using System.Threading;

namespace Echo.Core.Compute;

public abstract class Operation
{
	ulong currentProcedure;

	public virtual void Prepare()
	{
		Interlocked.Exchange(ref currentProcedure, 0);
	}

	/// <summary>
	/// Joins the execution of this <see cref="Operation"/> once.
	/// </summary>
	/// <param name="scheduler">The <see cref="Scheduler"/> used.</param>
	/// <returns>Whether this execution performed no work.</returns>
	public bool Execute(Scheduler scheduler)
	{
		ulong procedure = Interlocked.Increment(ref currentProcedure);
		return Execute(procedure - 1, scheduler);
	}

	protected abstract bool Execute(ulong procedure, Scheduler scheduler);
}