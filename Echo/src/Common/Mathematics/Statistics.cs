namespace Echo.Common.Mathematics;

/// <summary>
/// A specialized struct with the ability to quickly and cheaply count things.
/// </summary>
/// <remarks>The majority of the implementations are automatically generated.
/// See <see cref="Report"/> for the correct usage and limitation .</remarks>
public partial struct Statistics
{
	/// <summary>
	/// Retrieves information about a record.
	/// </summary>
	/// <param name="index">The numerical number of the target record. Must be
	/// between 0 (inclusive) and <see cref="Count"/> (exclusive). </param>
	/// <remarks>The counts contains the number of times an event occured.</remarks>
	public (string label, ulong count) this[int index] => IndexerImpl(index);

	/// <summary>
	/// The number of different records stored by <see cref="Statistics"/>.
	/// </summary>
	public static int Count => CountImpl();

	/// <summary>
	/// Notes down the occurrence of an event.
	/// </summary>
	/// <param name="label">The unique string label for that event; can only contain English letters,
	/// the 10 digits, spaces and slashes. This parameter *must* be a constant <see cref="string"/>,
	/// and can only be used in the main <see cref="Echo"/> project, otherwise it might not work properly.</param>
	/// <remarks>When used correctly, this method is extremely cheap.</remarks>
	public partial void Report(string label);

	/// <summary>
	/// Implementation for the indexer. 
	/// </summary>
	private partial (string label, ulong count) IndexerImpl(int index);

	/// <summary>
	/// Sums a number of <see cref="Statistics"/> together.
	/// </summary>
	/// <param name="source">The pointer to the series of <see cref="Statistics"/> in memory.</param>
	/// <param name="length">The number of <see cref="Statistics"/> to add together.</param>
	/// <returns>A new <see cref="Statistics"/> containing the sum.</returns>
	public static unsafe partial Statistics Sum(Statistics* source, int length);

	/// <summary>
	/// Implementation for the static property <see cref="Count"/>. Partial properties are
	/// not allowed in C# (for now: https://github.com/dotnet/csharplang/discussions/3412)
	/// </summary>
	private static partial int CountImpl();
}