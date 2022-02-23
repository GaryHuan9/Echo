using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Aggregation.Preparation;

/// <summary>
/// A constant sized array of <see cref="NodeToken"/>s that is divided into multiple partitions. Can retrieve
/// a <see cref="ReadOnlySpan{T}"/> of either the entire <see cref="NodeTokenArray"/> or individual partitions.
/// </summary>
public class NodeTokenArray
{
	/// <summary>
	/// Constructs a new <see cref="NodeTokenArray"/> with <paramref name="lengths.Length"/> partitions.
	/// The size of each partition is defined by each element value in <paramref name="lengths"/>.
	/// </summary>
	public NodeTokenArray(ReadOnlySpan<int> lengths)
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
	/// Returns the number of partitions in this <see cref="NodeTokenArray"/>.
	/// </summary>
	public int PartitionLength => starts.Length;

	/// <summary>
	/// Returns the total number of elements in this <see cref="NodeTokenArray"/>.
	/// </summary>
	public int TotalLength => array.Length;

	/// <summary>
	/// Returns the index of the final partition in this <see cref="NodeTokenArray"/>.
	/// </summary>
	public int FinalPartition => PartitionLength - 1;

	/// <summary>
	/// Returns whether this <see cref="NodeTokenArray"/> is completely filled up.
	/// </summary>
	public bool IsFull
	{
		get
		{
			for (int i = 0; i < heads.Length; i++)
			{
				if (GetSegmentEnd(i) != InterlockedHelper.Read(ref heads[i])) return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Returns the <paramref name="segment"/> partition of this <see cref="NodeTokenArray"/>.
	/// </summary>
	public ReadOnlySpan<NodeToken> this[int segment] => array.AsSpan(starts[segment]..InterlockedHelper.Read(ref heads[segment]));

	/// <summary>
	/// Adds a <see cref="NodeToken"/> into this <see cref="NodeTokenArray"/>'s <paramref name="segment"/> partition.
	/// Returns the global index of this new <see cref="NodeToken"/> relative to this <see cref="NodeTokenArray"/>.
	/// This method is thread safe.
	/// </summary>
	public int Add(int segment, in NodeToken token)
	{
		int index = Interlocked.Increment(ref heads[segment]) - 1;

		if (index >= GetSegmentEnd(segment)) throw ExceptionHelper.Invalid(nameof(segment), segment, "is already completely filled up");

		array[index] = token;
		return index;
	}

	int GetSegmentEnd(int segment) => segment == FinalPartition ? TotalLength : starts[segment + 1];

	/// <summary>
	/// Retrieves read access to the entirety of <paramref name="array"/>. Note that some
	/// elements might be uninitialized if <see cref="Add"/> is not invoked sufficiently.
	/// </summary>
	public static implicit operator ReadOnlySpan<NodeToken>(NodeTokenArray array) => array.array;
}