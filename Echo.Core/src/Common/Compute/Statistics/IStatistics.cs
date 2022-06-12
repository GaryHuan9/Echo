namespace Echo.Core.Common.Compute.Statistics;

/// <summary>
/// A specialized interface with the ability to quickly and cheaply count things.
/// Within this interface, an event consists of a label and a count, and this interface
/// keeps tracks of how many times each event has been seen.
/// </summary>
/// <typeparam name="T">Should be the type itself (ie. the type that is implementing this interface).</typeparam>
/// <remarks>The implementation of this interface can be automatically generated. See
/// the <see cref="GeneratedStatisticsAttribute"/> for more details and usage.</remarks>
// ReSharper disable once TypeParameterCanBeVariant
public interface IStatistics< /*should not be made as covariant!*/ T> where T : unmanaged, IStatistics<T>
{
	/// <summary>
	/// The number of different events tracked by this <see cref="IStatistics{T}"/>.
	/// </summary>
	int Count { get; } //OPTIMIZE: convert to static property when we upgrade to dotnet 7

	/// <summary>
	/// Retrieves recorded information about a event.
	/// </summary>
	/// <param name="index">The numerical number of the target event. Must be
	/// between 0 (inclusive) and <see cref="Count"/> (exclusive). </param>
	EventRow this[int index] { get; }

	/// <summary>
	/// Notes down the occurrence of an event.
	/// </summary>
	/// <param name="label">The unique string identifier for that event. Only the
	/// English characters, the 10 digits, spaces and slashes are allowed.</param>
	/// <param name="count">The number of times to note down.</param>
	/// <remarks>If this <see cref="IStatistics{T}"/> is automatically generated, the following *must* be followed:
	/// <para>(1) The input to parameter <paramref name="label"/> is a literal constant <see cref="string"/> that is
	/// passed directly. (2) Invocations can only occur within the main <see cref="Echo"/> project. (3) The invocation
	/// expression is in the form <c>stats.Report("item");</c> (ie. <c>array[0].Report("item");</c> is not allowed)</para>
	/// Otherwise, this method might not work properly.</remarks>
	void Report(string label, ulong count = 1);

	/// <summary>
	/// Sums a number of <see cref="IStatistics{T}"/>.
	/// </summary>
	/// <param name="source">A pointer to the series of <see cref="IStatistics{T}"/> to be summed together..</param>
	/// <param name="length">The number of <see cref="IStatistics{T}"/> to add together; must be positive.</param>
	/// <returns>A new <see cref="IStatistics{T}"/> containing the sum.</returns>
	unsafe T Sum(T* source, int length); //OPTIMIZE: convert to static method when we upgrade to dotnet 7
}