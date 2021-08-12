using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;

namespace IntrinsicsSIMD
{
	public class BenchmarkLoop
	{
		public BenchmarkLoop()
		{
			Random random = new Random(42);
			Span<byte> bytes = stackalloc byte[8];

			array = new long[1 << 20];

			for (int i = 0; i < array.Length; i++)
			{
				random.NextBytes(bytes);
				long value = BitConverter.ToInt64(bytes);

				array[i] = value;
				list.Add(value);
			}

			readOnlyCollection = new ReadOnlyCollection<long>(array);
			readOnlyList = array;
		}

		readonly long[] array;
		readonly List<long> list = new List<long>();
		readonly ReadOnlyCollection<long> readOnlyCollection;
		readonly IReadOnlyList<long> readOnlyList;

		// |                    Method |       Mean |    Error |   StdDev |
		// |-------------------------- |-----------:|---------:|---------:|
		// |                 ArrayFor0 |   500.3 us |  6.12 us |  5.72 us |
		// |                  ListFor1 |   497.2 us |  3.29 us |  2.92 us |
		// |    ReadOnlyCollectionFor0 | 3,935.2 us | 18.21 us | 17.03 us |
		// |    ReadOnlyCollectionFor1 | 2,459.7 us | 15.18 us | 14.20 us |
		// |          ReadOnlyListFor0 | 3,934.9 us | 39.03 us | 36.51 us |
		// |          ReadOnlyListFor1 | 1,974.6 us | 10.14 us |  9.49 us | <- better
		// |              ArrayForEach |   489.4 us |  2.48 us |  2.32 us | <- fastest
		// |               ListForEach | 1,915.0 us | 12.00 us | 11.22 us |
		// | ReadOnlyCollectionForEach | 3,687.1 us | 14.52 us | 12.87 us |
		// |       ReadOnlyListForEach | 3,702.7 us | 28.39 us | 23.70 us |

		[Benchmark]
		public long ArrayFor0()
		{
			long sum = 0L;

			for (int i = 0; i < array.Length; i++)
			{
				sum += array[i];
			}

			return sum;
		}

		// [Benchmark]
		public long ArrayFor1()
		{
			long sum = 0L;
			int length = array.Length;

			for (int i = 0; i < length; i++)
			{
				sum += array[i];
			}

			return sum;
		}

		// [Benchmark]
		public long ListFor0()
		{
			long sum = 0L;

			for (int i = 0; i < list.Count; i++)
			{
				sum += list[i];
			}

			return sum;
		}

		[Benchmark]
		public long ListFor1()
		{
			long sum = 0L;
			int count = list.Count;

			for (int i = 0; i < count; i++)
			{
				sum += list[i];
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyCollectionFor0()
		{
			long sum = 0L;

			for (int i = 0; i < readOnlyCollection.Count; i++)
			{
				sum += readOnlyCollection[i];
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyCollectionFor1()
		{
			long sum = 0L;
			int count = readOnlyCollection.Count;

			for (int i = 0; i < count; i++)
			{
				sum += readOnlyCollection[i];
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyListFor0()
		{
			long sum = 0L;

			for (int i = 0; i < readOnlyList.Count; i++)
			{
				sum += readOnlyList[i];
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyListFor1()
		{
			long sum = 0L;
			int count = readOnlyList.Count;

			for (int i = 0; i < count; i++)
			{
				sum += readOnlyList[i];
			}

			return sum;
		}

		[Benchmark]
		public long ArrayForEach()
		{
			long sum = 0L;

			foreach (long value in array)
			{
				sum += value;
			}

			return sum;
		}

		[Benchmark]
		public long ListForEach()
		{
			long sum = 0L;

			foreach (long value in list)
			{
				sum += value;
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyCollectionForEach()
		{
			long sum = 0L;

			foreach (long value in readOnlyCollection)
			{
				sum += value;
			}

			return sum;
		}

		[Benchmark]
		public long ReadOnlyListForEach()
		{
			long sum = 0L;

			foreach (long value in readOnlyList)
			{
				sum += value;
			}

			return sum;
		}
	}
}