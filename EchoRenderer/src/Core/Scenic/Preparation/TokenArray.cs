using System;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Scenic.Preparation;

/// <summary>
/// A constant sized array of <see cref="NodeToken"/>s that is divided into multiple partitions. Can retrieve
/// a <see cref="ReadOnlySpan{T}"/> of either the entire <see cref="TokenArray"/> or individual partitions.
/// </summary>
public class TokenArray
{
	/// <summary>
	/// Constructs a new <see cref="TokenArray"/> with <paramref name="lengths.Length"/> partitions.
	/// The size of each partition is defined by each element value in <paramref name="lengths"/>.
	/// </summary>
	public TokenArray(ReadOnlySpan<int> lengths)
	{
		//Calculate partition start indices using rolling sum
		int rolling = 0;

		starts = new int[lengths.Length];

		for (int i = 0; i < lengths.Length; i++)
		{
			int length = lengths[i];
			Assert.IsTrue(length > 0);

			starts[i] = rolling;
			rolling += length;
		}

		//Create array
		array = new NodeToken[rolling];
		heads = starts.ToArray();
	}

	readonly NodeToken[] array;
	readonly int[] starts;
	readonly int[] heads;

	/// <summary>
	/// Returns the <paramref name="segment"/> partition of this <see cref="TokenArray"/>.
	/// </summary>
	public ReadOnlySpan<NodeToken> this[int segment] => array.AsSpan(starts[segment]..GetSegmentEnd(segment));

	/// <summary>
	/// Adds a <see cref="NodeToken"/> into this <see cref="TokenArray"/>'s <paramref name="segment"/> partition.
	/// Returns the global index of this new <see cref="NodeToken"/> relative to this <see cref="TokenArray"/>.
	/// </summary>
	public int Add(int segment, in NodeToken token)
	{
		ref int head = ref heads[segment];
		int index = Interlocked.Increment(ref head) - 1;

		if (index >= GetSegmentEnd(segment)) throw ExceptionHelper.Invalid(nameof(segment), segment, "is already completely filled up");

		array[index] = token;
		return index;
	}

	int GetSegmentEnd(int segment) => segment + 1 == starts.Length ? array.Length : starts[segment + 1];

	public static implicit operator ReadOnlySpan<NodeToken>(TokenArray array) => array.array;
}