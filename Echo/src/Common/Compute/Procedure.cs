namespace Echo.Common.Compute;

/// <summary>
/// A step in an <see cref="Operation"/>.
/// </summary>
public struct Procedure
{
	public Procedure(uint index)
	{
		this.index = index;
		completedWork = 0;
		totalWorkR = 0f;
	}

	/// <summary>
	/// A value between 0 (inclusive) and <see cref="Operation.totalProcedureCount"/>
	/// (exclusive), indicating the specific step in an <see cref="Operation"/>.
	/// </summary>
	/// <remarks>This value will gradually increase as an <see cref="Operation"/> gets completed.</remarks>
	public readonly uint index;

	uint completedWork;
	double totalWorkR;

	/// <summary>
	/// The progress of this <see cref="Procedure"/>.
	/// </summary>
	/// <remarks>This value is between zero and one (both inclusive).</remarks>
	public readonly double Progress => totalWorkR * completedWork;

	/// <summary>
	/// Invoked before this <see cref="Procedure"/> is being worked on.
	/// </summary>
	/// <param name="totalWork">The total amount of work for the said step.</param>
	/// <remarks>It is optional to invoke this method, just like <see cref="Advance"/>.
	/// They are only used for correctly updating <see cref="Progress"/>.</remarks>
	public void Begin(uint totalWork) => totalWorkR = 1d / totalWork;

	/// <summary>
	/// Invoked when some progress has been made to this <see cref="Procedure"/>.
	/// </summary>
	/// <param name="amount">The amount of progress made. The scale of this value
	/// is in conjecture with value passed to <see cref="Begin"/>.</param>
	/// <remarks>It is optional to invoke this method, just like <see cref="Begin"/>.
	/// They are only used for correctly updating <see cref="Progress"/>.</remarks>
	public void Advance(uint amount = 1) => completedWork += amount;
}