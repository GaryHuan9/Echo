using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;

namespace Echo.Experimental.Benchmarks;

public class Loops
{
	public Loops()
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
	readonly List<long> list = new();
	readonly ReadOnlyCollection<long> readOnlyCollection;
	readonly IReadOnlyList<long> readOnlyList;

	// |                    Method |       Mean |    Error |   StdDev |
	// |-------------------------- |-----------:|---------:|---------:|
	// |                 ArrayFor0 |   490.0 us |  2.77 us |  2.59 us |
	// |                 ArrayFor1 |   489.7 us |  3.15 us |  2.95 us |
	// |                  ListFor0 |   878.6 us |  4.18 us |  3.91 us |
	// |                  ListFor1 |   503.7 us |  3.15 us |  2.94 us |
	// |    ReadOnlyCollectionFor0 | 3,975.1 us | 12.28 us | 11.48 us |
	// |    ReadOnlyCollectionFor1 | 2,244.7 us | 13.25 us | 11.74 us |
	// |          ReadOnlyListFor0 | 4,230.2 us | 11.51 us | 10.77 us |
	// |          ReadOnlyListFor1 | 2,243.6 us |  6.55 us |  6.13 us | <- better
	// |              ArrayForEach |   343.1 us |  2.29 us |  2.15 us | <- fastest
	// |               ListForEach | 1,996.3 us |  6.61 us |  6.18 us |
	// | ReadOnlyCollectionForEach | 3,733.8 us | 16.07 us | 14.25 us |
	// |       ReadOnlyListForEach | 3,732.6 us | 16.93 us | 15.84 us |

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

	[Benchmark]
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

	[Benchmark]
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