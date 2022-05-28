namespace Echo.Common.Compute;

/// <summary>
/// A delegation into an <see cref="Operation"/> representing the <see cref="Worker"/> that is executing the said <see cref="Operation"/>.
/// </summary>
public interface IScheduler
{
	/// <summary>
	/// Two <see cref="IScheduler"/> with the same <see cref="Id"/> will never execute the
	/// same <see cref="Operation"/> at the same time. Additionally, the value of this property
	/// will start at zero and continues on for different <see cref="IScheduler"/>.
	/// </summary>
	public uint Id { get; }

	/// <summary>
	/// Checks if there are any schedule changes.
	/// </summary>
	/// <remarks>Should be invoked periodically during an <see cref="Operation"/>.</remarks>
	public void CheckSchedule();
}