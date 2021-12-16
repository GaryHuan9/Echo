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
			counts = new int[256];
			keys0 = new uint[capacity];
		}

		readonly int capacity;
		readonly int[] counts;
		readonly uint[] keys0;

		uint[] keys1;
		int[] buffer;

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
				case > 8: //TODO: change to something higher once we add quick sort
				{
					keys1 ??= new uint[capacity];
					buffer ??= new int[capacity];

					var sorter = new RadixSorter(counts, keys0, keys1, indices, buffer);
					sorter.Sort();

					break;
				}
				default:
				{
					var sorter = new InsertionSorter(keys0, indices);
					sorter.Sort();
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

		readonly ref struct InsertionSorter
		{
			public InsertionSorter(Span<uint> keys, Span<int> values)
			{
				length = values.Length;
				Assert.IsTrue(keys.Length >= length);

				this.keys = keys;
				this.values = values;
			}

			readonly int length;
			readonly Span<uint> keys;
			readonly Span<int> values;

			public void Sort()
			{
				//Run for the entire length
				for (int i = 1; i < length; i++)
				{
					uint key = keys[i];

					//Look back and search for appropriate position
					for (int j = i; j > 0; j--)
					{
						uint next = keys[j - 1];

						if (key < next)
						{
							//Shift keys forward
							keys[j] = next;
							values[j] = values[j - 1];
						}
						else
						{
							//Found position, break
							if (j == i) break;

							keys[j] = key;
							values[j] = values[i];

							break;
						}
					}
				}
			}
		}

		ref struct RadixSorter
		{
			public RadixSorter(Span<int> counts, Span<uint> keys0, Span<uint> keys1, Span<int> values0, Span<int> values1)
			{
				Assert.AreEqual(counts.Length, 256);
				this.counts = counts;

				length = values0.Length;

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
						Swap(ref values1, ref values0);
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
					values1[index] = values0[index];
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void RebuildValuesOnly(int shift)
			{
				for (int i = length - 1; i >= 0; i--)
				{
					ref readonly uint key = ref keys0[i];
					int index = --counts[(byte)(key >> shift)];
					values1[index] = values0[index];
				}
			}
		}
	}
}