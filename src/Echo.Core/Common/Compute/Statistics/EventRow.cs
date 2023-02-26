namespace Echo.Core.Common.Compute.Statistics;

/// <summary>
/// A small value type containing information about an event.
/// </summary>
public readonly record struct EventRow(string Label, ulong Count)
{
	/// <summary>
	/// The <see cref="string"/> label of this event.
	/// </summary>
	public string Label { get; init; } = Label;

	/// <summary>
	/// The number of occurrences for this event.
	/// </summary>
	public ulong Count { get; init; } = Count;
}