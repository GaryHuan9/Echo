using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Acceleration;

using BoundsView = View<Tokenized<BoxBound>>;

public class SweepBuilder : HierarchyBuilder
{
	public SweepBuilder(BoundsView bounds) : base(bounds) => sorter = new Sorter(bounds.Length);

	readonly Sorter sorter;

	BoxBound[] cutTailVolumes;

	const int ParallelBuildThreshold = 4096; //We can increase this value to disable parallel building

	public override Node Build()
	{
		if (bounds.Length == 1) return new Node(bounds[0]);

		var builder = BoxBound.CreateBuilder();

		foreach (ref readonly var pair in bounds) builder.Add(pair.content);

		int axis = builder.ToBoxBound().MajorAxis;

		SortIndices(bounds, axis);
		return BuildLayer(bounds);
	}

	Node BuildLayer(BoundsView data)
	{
		Assert.IsFalse(data.Length < 2);
		PrepareCutTailVolumes(data);

		int minIndex = SearchSurfaceAreaHeuristics(data, out var headVolume, out var tailVolume);
		BoxBound bound = headVolume.Encapsulate(tailVolume);

		int axis = bound.MajorAxis;

		//Split data based on minIndex; headData is always larger than tailData
		BoundsView headData;
		BoundsView tailData;

		if (minIndex > data.Length / 2)
		{
			headData = data[..minIndex];
			tailData = data[minIndex..];
		}
		else
		{
			headData = data[minIndex..];
			tailData = data[..minIndex];

			CodeHelper.Swap(ref headVolume, ref tailVolume);
		}

		//Recursively construct deeper layers
		Node child0;
		Node child1;

		if (headData.Length < ParallelBuildThreshold)
		{
			child0 = BuildChild(headData, headVolume, axis);
			child1 = BuildChild(tailData, tailVolume, axis);
		}
		else
		{
			var builder = BuildChildParallel(headData, headVolume, axis);
			child1 = BuildChild(tailData, tailVolume, axis);
			child0 = builder.WaitForNode();
		}

		//Places the child with the larger surface area first to improve branch prediction
		if (headVolume.HalfArea < tailVolume.HalfArea) CodeHelper.Swap(ref child0, ref child1);

		return new Node(bound, child0, child1, axis);
	}

	Node BuildChild(BoundsView data, in BoxBound parent, int parentAxis)
	{
		if (data.Length == 1) return new Node(data[0]);

		int axis = parent.MajorAxis;
		if (axis != parentAxis) SortIndices(data, axis);
		return BuildLayer(data);
	}

	LayerBuilder BuildChildParallel(BoundsView data, in BoxBound parent, int parentAxis)
	{
		Assert.IsTrue(data.Length > 1);

		int axis = parent.MajorAxis;
		if (axis == parentAxis) axis = -1; //No need to sort because it is already sorted
		return new LayerBuilder(data, axis);
	}

	/// <summary>
	/// Sorts a <see cref="BoundsView"/> by an axis based on the location of the bounds on that axis.
	/// </summary>
	void SortIndices(BoundsView data, int axis) => sorter.Sort(data, axis);

	/// <summary>
	/// Calculates all of the tail volumes and stores them to <see cref="cutTailVolumes"/>.
	/// The prepared data is then used by <see cref="SearchSurfaceAreaHeuristics"/>.
	/// </summary>
	void PrepareCutTailVolumes(BoundsView data)
	{
		int length = data.Length;

		cutTailVolumes ??= new BoxBound[length];
		BoxBound cutTailVolume = data[^1].content;

		for (int i = length - 2; i >= 0; i--)
		{
			cutTailVolumes[i + 1] = cutTailVolume;
			cutTailVolume = cutTailVolume.Encapsulate(data[i].content);
		}
	}

	/// <summary>
	/// Searches the length of <paramref name="data"/> to find and return the spot where the SAH is the lowest.
	/// Also returns the two volumes after cutting at the returned index. NOTE: Uses the prepared <see cref="cutTailVolumes"/>.
	/// </summary>
	int SearchSurfaceAreaHeuristics(BoundsView data, out BoxBound headVolume, out BoxBound tailVolume)
	{
		BoxBound cutHeadVolume = data[0].content;

		float minCost = float.MaxValue;
		int minIndex = -1;

		headVolume = default;
		tailVolume = default;

		int length = data.Length;

		for (int i = 1; i < length; i++)
		{
			ref readonly BoxBound cutTailVolume = ref cutTailVolumes[i];
			float cost = cutHeadVolume.HalfArea * i + cutTailVolume.HalfArea * (length - i);

			if (cost < minCost)
			{
				minCost = cost;
				minIndex = i;

				headVolume = cutHeadVolume;
				tailVolume = cutTailVolume;
			}

			cutHeadVolume = cutHeadVolume.Encapsulate(data[i].content);
		}

		return minIndex;
	}

	class LayerBuilder
	{
		public LayerBuilder(BoundsView bounds, int sortAxis)
		{
			this.bounds = bounds;
			this.sortAxis = sortAxis;

			buildTask = Task.Run(Build);
		}

		readonly Task<Node> buildTask;
		readonly BoundsView bounds;
		readonly int sortAxis;

		public Node WaitForNode() => buildTask.Result;

		Node Build()
		{
			var builder = new SweepBuilder(bounds);

			//Sort indices if requested by parent
			if (sortAxis >= 0) builder.SortIndices(bounds, sortAxis);

			return builder.BuildLayer(bounds);
		}
	}

	/// <summary>
	/// A sorter that sorts <see cref="BoundsView"/> based on the center locations of the <see cref="BoxBound"/> they contain.
	/// </summary>
	class Sorter
	{
		public Sorter(int capacity)
		{
			this.capacity = capacity;
			keys0 = new uint[capacity];
		}

		readonly int capacity;
		readonly uint[] keys0;

		int[] counts;
		uint[] keys1;
		BoundsView buffer;

		/// <summary>
		/// Sorts <paramref name="bounds"/> based on the <see cref="axis"/> location value of the corresponding <see cref="BoxBound"/>.
		/// </summary>
		/// <remarks><paramref name="bounds.Length"/> must be equal to or less than <see cref="capacity"/></remarks>
		public void Sort(BoundsView bounds, int axis)
		{
			int length = bounds.Length;
			Assert.IsFalse(length > capacity);

			//Fetch and transform locations into key buffers
			for (int i = 0; i < length; i++)
			{
				ref readonly var bound = ref bounds[i].content;
				float value = bound.min[axis] + bound.max[axis];

				keys0[i] = Transform(value);
			}

			//Sort using the best strategy
			switch (length)
			{
				case > 32: //TODO: change to something higher once we add quick sort
				{
					if (counts == null)
					{
						counts = new int[256];
						keys1 = new uint[capacity];
						buffer = new Tokenized<BoxBound>[capacity];
					}

					var sorter = new RadixSorter(counts, keys0, keys1, bounds, buffer);
					sorter.Sort();

					break;
				}
				default:
				{
					InsertionSort(keys0.AsSpan(0, length), bounds);
					break;
				}
			}
		}

		/// <summary>
		/// Transforms <paramref name="value"/> into a uint which can be radix sorted. Because negative IEEE-754 floating point values have their
		/// most significant bits on, we need to flip either just the most significant bit if the value is positive, or all the bits otherwise.
		/// </summary>
		static uint Transform(float value)
		{
			const uint HeadBit = 1u << 31;
			const uint AllBits = ~0u;

			//Conditionally flip some bits
			uint converted = Scalars.SingleToUInt32Bits(value);
			uint flip = (converted >> 31) * (AllBits - HeadBit);

			return converted ^ (flip + HeadBit);
		}

		/// <summary>
		/// A simple stable insertion sort that acts on <paramref name="keys"/> and <paramref name="values"/> pairs.
		/// </summary>
		static void InsertionSort(Span<uint> keys, BoundsView values)
		{
			Assert.AreEqual(keys.Length, values.Length);

			//Run for every key value pair except the first one
			for (int i = 1; i < keys.Length; i++)
			{
				uint key = keys[i];
				var value = values[i];

				//Look back and search for appropriate position
				for (int scan = i;; --scan)
				{
					if (scan > 0 && key < keys[scan - 1])
					{
						//Shift keys forward
						keys[scan] = keys[scan - 1];
						values[scan] = values[scan - 1];
					}
					else
					{
						//Found position, stop
						if (scan == i) break;

						keys[scan] = key;
						values[scan] = value;

						break;
					}
				}
			}
		}

		ref struct RadixSorter
		{
			public RadixSorter(Span<int> counts, Span<uint> keys0, Span<uint> keys1, Span<Tokenized<BoxBound>> values0, Span<Tokenized<BoxBound>> values1)
			{
#if DEBUG
				Span<int> empty = stackalloc int[256];
				empty.Clear();
				Assert.IsTrue(counts.SequenceEqual(empty));
#endif

				length = values0.Length;
				this.counts = counts;

				this.keys0 = keys0[..length];
				this.keys1 = keys1[..length];

				this.values0 = values0[..length];
				this.values1 = values1[..length];
			}

			readonly int length;
			readonly Span<int> counts;

			Span<uint> keys0;
			Span<uint> keys1;

			Span<Tokenized<BoxBound>> values0;
			Span<Tokenized<BoxBound>> values1;

			public void Sort()
			{
				//Run four rounds
				for (int i = 0; i < 4; i++)
				{
					int shift = i * 8;

					GatherCounts(shift);
					CreatePrefixSums();

					//Check whether this is the final round
					if (i < 3)
					{
						RebuildKeysAndValues(shift);
						Swap(ref keys0, ref keys1);
						Swap(ref values0, ref values1);
					}
					else RebuildValuesOnly(shift);

					counts.Clear();
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void GatherCounts(int shift)
			{
				for (int i = 0; i < length; i++)
				{
					uint digit = keys0[i] >> shift;
					++counts[(byte)digit];
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void CreatePrefixSums()
			{
				int sum = 0;

				foreach (ref int count in counts)
				{
					count += sum;
					sum = count;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void RebuildKeysAndValues(int shift)
			{
				for (int i = length - 1; i >= 0; i--)
				{
					ref readonly uint key = ref keys0[i];
					int index = --counts[(byte)(key >> shift)];

					keys1[index] = key;
					values1[index] = values0[i];
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void RebuildValuesOnly(int shift)
			{
				for (int i = length - 1; i >= 0; i--)
				{
					ref readonly uint key = ref keys0[i];
					int index = --counts[(byte)(key >> shift)];
					values1[index] = values0[i];
				}
			}

			/// <summary>
			/// Swaps two <see cref="Span{T}"/>.
			/// </summary>
			static void Swap<T>(ref Span<T> span0, ref Span<T> span1)
			{
				var storage = span0;
				span0 = span1;
				span1 = storage;
			}
		}
	}
}