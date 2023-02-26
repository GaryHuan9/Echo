using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Experimental.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
public class MathFunctions
{
	public MathFunctions()
	{
		array = new float[1024 * 1024];
		Prng random = new SystemPrng(42);

		for (int i = 0; i < array.Length; i++) array[i] = random.Next1(10f);
	}

	readonly float[] array;

	// Input range 0 - 100
	// |                 Method |        Mean |     Error |    StdDev | Ratio | RatioSD |
	// |----------------------- |------------:|----------:|----------:|------:|--------:|
	// |               Overhead |    718.3 us |   6.97 us |   6.52 us |  1.01 |    0.01 |
	// |                    Add |    715.2 us |   3.37 us |   2.81 us |  1.00 |    0.00 |
	// |               Multiply |    713.5 us |   4.44 us |   4.15 us |  1.00 |    0.01 |
	// |             Reciprocal |    834.5 us |   8.29 us |   7.76 us |  1.17 |    0.01 |
	// |         ReciprocalSqrt |  2,216.3 us |  10.97 us |  10.27 us |  3.10 |    0.02 |
	// |     EstimateReciprocal |    715.1 us |   4.68 us |   4.38 us |  1.00 |    0.01 |
	// | EstimateReciprocalSqrt |    719.5 us |   5.26 us |   4.92 us |  1.01 |    0.01 |
	// |             Conversion |    727.9 us |   6.34 us |   5.93 us |  1.02 |    0.01 |
	// |            SqrtFloat64 |  2,066.6 us |  12.80 us |  10.69 us |  2.89 |    0.02 |
	// |            SqrtFloat32 |  1,301.9 us |   6.92 us |   6.14 us |  1.82 |    0.01 |
	// |                   Cbrt | 30,367.7 us | 319.49 us | 298.85 us | 42.41 |    0.40 |
	// |                 Cosine | 11,445.5 us | 103.09 us |  96.43 us | 16.00 |    0.16 |
	// |              Arccosine | 53,868.2 us | 565.67 us | 501.45 us | 75.24 |    0.69 |
	// |                    Exp | 11,330.3 us |  81.30 us |  76.05 us | 15.82 |    0.07 |
	// |                    Log |  4,248.1 us |  24.98 us |  22.14 us |  5.94 |    0.04 |
	// |                   Log2 | 27,101.2 us | 128.07 us | 119.80 us | 37.87 |    0.28 |
	// |                MathAbs |    725.8 us |   3.42 us |   3.20 us |  1.01 |    0.01 |
	// |            FastMathAbs |    722.7 us |   4.23 us |   3.95 us |  1.01 |    0.00 |

	[Benchmark]
	public float Overhead()
	{
		float result = 0f;

		foreach (float value in array) result += value;

		return result;
	}

	[Benchmark(Baseline = true)]
	public float Add()
	{
		float result = 0f;

		foreach (float value in array) result += value + value;

		return result;
	}

	[Benchmark]
	public float Multiply()
	{
		float result = 0f;

		foreach (float value in array) result += value * value;

		return result;
	}

	[Benchmark]
	public float Reciprocal()
	{
		float result = 0f;

		foreach (float value in array) result += 1f / value;

		return result;
	}

	[Benchmark]
	public float ReciprocalSqrt()
	{
		float result = 0f;

		foreach (float value in array) result += 1f / MathF.Sqrt(value);

		return result;
	}

	[Benchmark]
	public float EstimateReciprocal()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.ReciprocalEstimate(value);

		return result;
	}

	[Benchmark]
	public float EstimateReciprocalSqrt()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.ReciprocalSqrtEstimate(value);

		return result;
	}

	[Benchmark]
	public float Conversion()
	{
		float result = 0f;

		foreach (float value in array) result += (int)value; //NOTE: there are two conversions here!

		return result;
	}

	[Benchmark]
	public float SqrtFloat64()
	{
		float result = 0f;

		foreach (float value in array) result += (float)Math.Sqrt(value);

		return result;
	}

	[Benchmark]
	public float SqrtFloat32()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Sqrt(value);

		return result;
	}

	[Benchmark]
	public float Cbrt()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Cbrt(value);

		return result;
	}

	[Benchmark]
	public float Cosine()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Cos(value);

		return result;
	}

	[Benchmark]
	public float Arccosine()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Acos(value);

		return result;
	}

	[Benchmark]
	public float Exp()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Exp(value);

		return result;
	}

	[Benchmark]
	public float Log()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Log(value);

		return result;
	}

	[Benchmark]
	public float Log2()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Log2(value);

		return result;
	}

	[Benchmark]
	public float MathAbs()
	{
		float result = 0f;

		foreach (float value in array) result += Math.Abs(value);

		return result;
	}

	[Benchmark]
	public float FastMathAbs()
	{
		float result = 0f;

		foreach (float value in array) result += FastMath.Abs(value);

		return result;
	}
}