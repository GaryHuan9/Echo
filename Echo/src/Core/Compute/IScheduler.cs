namespace Echo.Core.Compute;

public interface IScheduler
{
	/// <summary>
	/// Two <see cref="IScheduler"/> with the same <see cref="Id"/> will never execute at the same time.
	/// </summary>
	public uint Id { get; }

	public void CheckSchedule();
}