using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace IntrinsicsSIMD
{
	public class BenchmarkLoop
	{
		public BenchmarkLoop()
		{
			Random random = new Random(42);
			Span<byte> bytes = stackalloc byte[8];

			array = new long[16777216];

			for (int i = 0; i < array.Length; i++)
			{
				random.NextBytes(bytes);
				long value = BitConverter.ToInt64(bytes);

				array[i] = value;
				list.Add(value);
				hashSet.Add(value);
			}
		}

		readonly long[] array;
		readonly List<long> list = new List<long>();
		readonly HashSet<long> hashSet = new HashSet<long>();

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
		public long HashSetForEach()
		{
			long sum = 0L;

			foreach (long value in hashSet)
			{
				sum += value;
			}

			return sum;
		}
	}
}