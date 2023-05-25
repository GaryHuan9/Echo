using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Memory;

namespace Echo.Experimental.Benchmarks;

public class RenameThisBenchmark
{
	public RenameThisBenchmark()
	{
		//Generate values and samples
		var random = new SystemPrng(42);
		float[] values = new float[2000];

		foreach (ref float value in values.AsSpan()) value = random.Next1();
		foreach (ref float sample in samples.AsSpan()) sample = random.Next1();

		values.AsSpan().Sort();
		values[^1] = 1f;

		Searchers.Add(new Original(values));
		//TODO: add more

		//Test all searchers
		foreach (Searcher searcher in Searchers) TestSearcher(searcher, values);
	}

	[ParamsSource(nameof(Searchers))]
	public Searcher Current { get; set; }

	public List<Searcher> Searchers { get; } = new();

	readonly float[] samples = new float[1000];

	[Benchmark]
	public int FindIndex()
	{
		int result = 0;

		foreach (float sample in samples) result ^= Current.FindIndex(sample);

		return result;
	}

	static void TestSearcher(Searcher searcher, ReadOnlySpan<float> values)
	{
		//Test exact matches
		for (int i = 0; i < values.Length; i++)
		{
			int result = searcher.FindIndex(values[i]);
			ThrowIfNotEqual(values[result], values[i]);
		}

		//Test other samples
		int length = values.Length * 10;

		for (int i = 0; i < length; i++)
		{
			float sample = (float)i / length;
			int result = searcher.FindIndex(sample);
			int reference = values.BinarySearch(sample);
			ThrowIfNotEqual(result, reference);
		}

		static void ThrowIfNotEqual<T>(T value, T other) where T : IEquatable<T>
		{
			if (value.Equals(other)) return;
			throw new ArgumentException($"{value} != {other}");
		}
	}

	public abstract class Searcher
	{
		protected Searcher(ReadOnlySpan<float> values) => array = new AlignedArray<float>(values);

		/// <summary>
		/// An <see cref="AlignedArray{T}"/> of never-decreasing float values.
		/// </summary>
		protected readonly AlignedArray<float> array;

		/// <summary>
		/// Look for the index of the value that equals <paramref name="sample"/>.
		/// </summary>
		/// <remarks>If a value does exactly match <paramref name="sample"/>, its index is simply returned.
		/// If no value matches <paramref name="sample"/> exactly (which is much more likely), the bitwise
		/// inverted index (ie. ~index) of the smallest value that is bigger than <see cref="sample"/> is
		/// returned. If there are multiple identical matching values, returning any index is sufficient.
		/// For example, when array = {2, 4, 7}, a sample of 4 should return 1, a sample of 5 should return
		/// ~2, a sample of 1 should return ~0, and a sample of 8 can be undefined behavior.</remarks>
		public abstract int FindIndex(float sample);
	}

	class Original : Searcher
	{
		public Original(ReadOnlySpan<float> values) : base(values) { }

		public override int FindIndex(float sample)
		{
			uint head = 0u;
			uint tail = (uint)array.Length;
			ref float origin = ref array[0];

			while (head < tail)
			{
				uint index = (tail + head) >> 1;

				var current = Unsafe.Add(ref origin, index);
				if (current == sample) return (int)index;

				if (current > sample) tail = index;
				else head = index + 1u;
			}

			return (int)~head;
		}
	}
}