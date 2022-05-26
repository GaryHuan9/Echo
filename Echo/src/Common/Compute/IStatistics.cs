namespace Echo.Common.Compute;

/// <summary>
/// A specialized interface with the ability to quickly and cheaply count things.
/// Within this interface, an event consists of a label and a count, and this interface
/// keeps tracks of how many times each event has been seen.
/// </summary>
/// <typeparam name="T">Should be the type itself (ie. the type that is implementing this interface.</typeparam>
/// <remarks>The implementation of this interface can be automatically generated. See
/// the <see cref="GeneratedStatisticsAttribute"/> for more details and usage.</remarks>
public interface IStatistics<T> where T : unmanaged, IStatistics<T>
{
	/// <summary>
	/// The number of different events tracked by this <see cref="IStatistics{T}"/>.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Retrieves recorded information about a event.
	/// </summary>
	/// <param name="index">The numerical number of the target event. Must be
	/// between 0 (inclusive) and <see cref="Count"/> (exclusive). </param>
	/// <remarks>The second value in the returned tuple (count) contains the number of recorded occurrence.</remarks>
	(string label, ulong count) this[int index] { get; }

	/// <summary>
	/// Notes down the occurrence of an event.
	/// </summary>
	/// <param name="label">The unique string identifier for that event. Only the
	/// English characters, the 10 digits, spaces and slashes are allowed.</param>
	/// <remarks> If this <see cref="IStatistics{T}"/> is automatically generated, <paramref name="label"/> *must* be
	/// a literal constant <see cref="string"/> passed directly into this method, and invocations to this method can
	/// only occur in the main <see cref="Echo"/> project, otherwise it might not work properly.</remarks>
	void Report(string label);

	/// <summary>
	/// Sums a number of <see cref="IStatistics{T}"/>.
	/// </summary>
	/// <param name="source">A pointer to the series of <see cref="IStatistics{T}"/> to be summed together..</param>
	/// <param name="length">The number of <see cref="IStatistics{T}"/> to add together; must be positive.</param>
	/// <remarks>The content of this <see cref="IStatistics{T}"/> will be replaced by the result of this summation.</remarks>
	unsafe void Sum(T* source, int length);
}