using System;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A constant sized array of <see cref="EntityToken"/>s that is divided into multiple partitions. Can retrieve
/// a <see cref="ReadOnlySpan{T}"/> of either the entire <see cref="EntityTokenArray"/> or individual partitions.
/// </summary>
public class EntityTokenArray
{
	/// <summary>
	/// Constructs a new <see cref="EntityTokenArray"/> with <paramref name="lengths.Length"/> partitions.
	/// The size of each partition is defined by each element value in <paramref name="lengths"/>.
	/// </summary>
	public EntityTokenArray(ReadOnlySpan<int> lengths)
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
		array = new EntityToken[rolling];
		heads = starts.ToArray();
	}

	readonly EntityToken[] array;
	readonly int[] starts;
	readonly int[] heads;

	/// <summary>
	/// Returns the number of partitions in this <see cref="EntityTokenArray"/>.
	/// </summary>
	public int PartitionLength => starts.Length;

	/// <summary>
	/// Returns the total number of elements in this <see cref="EntityTokenArray"/>.
	/// </summary>
	public int TotalLength => array.Length;

	/// <summary>
	/// Returns the index of the final partition in this <see cref="EntityTokenArray"/>.
	/// </summary>
	public int FinalPartition => PartitionLength - 1;

	/// <summary>
	/// Returns whether this <see cref="EntityTokenArray"/> is completely filled up.
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
	/// Returns the <paramref name="segment"/> partition of this <see cref="EntityTokenArray"/>.
	/// </summary>
	public ReadOnlyView<EntityToken> this[int segment] => array.AsView()[starts[segment]..InterlockedHelper.Read(ref heads[segment])];

	/// <summary>
	/// Adds an <see cref="EntityToken"/> into this <see cref="EntityTokenArray"/>'s <paramref name="segment"/> partition.
	/// Returns the global index of this new <see cref="EntityToken"/> relative to this <see cref="EntityTokenArray"/>.
	/// This method is thread safe.
	/// </summary>
	public int Add(int segment, EntityToken token)
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
	public static implicit operator ReadOnlySpan<EntityToken>(EntityTokenArray array) => array.array;
}