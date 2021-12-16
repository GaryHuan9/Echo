using System;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// A sorter that sorts indices based on the locations of <see cref="AxisAlignedBoundingBox"/> they point at.
	/// </summary>
	public class BoxSorter
	{
		public BoxSorter(int capacity)
		{
			this.capacity = capacity;
			keys0 = new uint[capacity];
		}

		readonly int capacity;
		readonly uint[] keys0;

		int[] counts;
		uint[] keys1;
		int[] buffer;

		/// <summary>
		/// Sorts <paramref name="indices"/> based on the <see cref="axis"/> location value of the corresponding <see cref="AxisAlignedBoundingBox"/>
		/// they point at. NOTE: <paramref name="indices.Length"/> must be equal to or less than <see cref="capacity"/>.
		/// </summary>
		public void Sort(ReadOnlySpan<AxisAlignedBoundingBox> aabbs, Span<int> indices, int axis)
		{
			int length = indices.Length;
			Assert.IsFalse(length > capacity);

			//Fetch and transform locations into key buffers
			for (int i = 0; i < length; i++)
			{
				ref readonly var aabb = ref aabbs[indices[i]];
				float value = aabb.min[axis] + aabb.max[axis];

				keys0[i] = Transform(value);
			}

			//Sort using the best strategy
			switch (length)
			{
				case > 32: //TODO: change to something higher once we add quick sort
				{
					counts ??= new int[256];
					keys1 ??= new uint[capacity];
					buffer ??= new int[capacity];

					var sorter = new RadixSorter(counts, keys0, keys1, indices, buffer);
					sorter.Sort();

					break;
				}
				default:
				{
					InsertionSort(keys0.AsSpan(0, indices.Length), indices);
					break;
				}
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
		static void InsertionSort(Span<uint> keys, Span<int> values)
		{
			Assert.AreEqual(keys.Length, values.Length);

			//Run for every key value pair except the first one
			for (int i = 1; i < keys.Length; i++)
			{
				uint key = keys[i];
				int value = values[i];

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
			public RadixSorter(Span<int> counts, Span<uint> keys0, Span<uint> keys1, Span<int> values0, Span<int> values1)
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

			Span<int> values0;
			Span<int> values1;

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
		}
	}
}