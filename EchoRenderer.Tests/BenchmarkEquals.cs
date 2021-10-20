using System;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Tests
{
	public class BenchmarkEquals
	{
		public BenchmarkEquals()
		{
			array = new float[1000];
			var random = new Random(42);

			for (int i = 0; i < array.Length; i++)
			{
				ref float value = ref array[i];
				value = (float)random.NextDouble();
			}
		}

		readonly float[] array;

		[Benchmark]
		public bool DoubleEqual()
		{
			bool result = default;

			foreach (float number0 in array)
			{
				foreach (float number1 in array)
				{
					result ^= number0 == number1;
				}
			}

			return result;
		}

		[Benchmark]
		public bool EpsilonEqual()
		{
			bool result = default;

			foreach (float number0 in array)
			{
				foreach (float number1 in array)
				{
					result ^= Math.Abs(number0 - number1) < Scalars.Epsilon;
				}
			}

			return result;
		}

		[Benchmark]
		public bool AlmostEqual()
		{
			bool result = default;

			foreach (float number0 in array)
			{
				foreach (float number1 in array)
				{
					result ^= number0.AlmostEquals(number1);
				}
			}

			return result;
		}

		[Benchmark]
		public bool AlmostEqualBetter()
		{
			bool result = default;

			foreach (float number0 in array)
			{
				foreach (float number1 in array)
				{
					result ^= AlmostEquals(number0, number1);
				}
			}

			return result;
		}

		static bool AlmostEquals(float value, float other, int delta = 3)
		{
			if (value == other) return true;

			if (float.IsNaN(value) || float.IsNaN(other)) return false;
			if (float.IsInfinity(value) || float.IsInfinity(value)) return false;

			int int0 = Scalars.SingleToInt32Bits(value);
			if ((int0 & 0x7FFFFFFF) == 0x7F800000) return false;

			int int1 = Scalars.SingleToInt32Bits(other);
			if ((int1 & 0x7FFFFFFF) == 0x7F800000) return false;

			if (int0 < 0) int0 = int.MinValue - int0;
			if (int1 < 0) int1 = int.MinValue - int1;

			return Math.Abs(int0 - int1) <= delta;
		}
	}
}