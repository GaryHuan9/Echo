using System;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;

namespace IntrinsicsSIMD
{
	public class BenchmarkLocalRef
	{
		public BenchmarkLocalRef()
		{
			Random random = new Random(42);
			Span<byte> bytes = stackalloc byte[4 * 4];

			array = new Int4[Length];

			for (int i = 0; i < Length; i++)
			{
				random.NextBytes(bytes);

				array[i] = new Int4
				(
					BitConverter.ToInt32(bytes[..4]),
					BitConverter.ToInt32(bytes[4..8]),
					BitConverter.ToInt32(bytes[8..12]),
					BitConverter.ToInt32(bytes[12..])
				);
			}

			collection = new ReadOnlyCollection<Int4>(array);
		}

		const int Length = 1 << 20;

		readonly Int4[] array;
		readonly ReadOnlyCollection<Int4> collection;

		[Benchmark]
		public Int4 ArrayForEach()
		{
			Int4 sum = default;

			foreach (Int4 value in array)
			{
				sum += value;
			}

			return sum;
		}

		[Benchmark]
		public Int4 ArrayFor()
		{
			Int4 sum = default;

			for (int i = 0; i < Length; i++)
			{
				sum += array[i];
			}

			return sum;
		}

		// [Benchmark]
		public Int4 CollectionFor()
		{
			Int4 sum = default;

			for (int i = 0; i < Length; i++)
			{
				sum += collection[i];
			}

			return sum;
		}

		// [Benchmark]
		public Int4 CollectionForEach()
		{
			Int4 sum = default;

			foreach (Int4 value in collection)
			{
				sum += value;
			}

			return sum;
		}
	}
}