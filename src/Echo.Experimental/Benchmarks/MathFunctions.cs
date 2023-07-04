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

		for (int i = 0; i < array.Length; i++)
		{
			float value = random.Next1(16f);
			array[i] = random.NextBoolean() ? value : -value;
		}
	}

	readonly float[] array;

	// Input range (16, -16) on AMD 3900x
	// |                 Method |        Mean |     Error |    StdDev | Ratio | RatioSD |
	// |----------------------- |------------:|----------:|----------:|------:|--------:|
	// |               Overhead |    702.2 us |   1.85 us |   1.73 us |  1.00 |    0.00 |
	// |                    Add |    703.9 us |   2.97 us |   2.63 us |  1.00 |    0.00 |
	// |               Multiply |    704.3 us |   3.48 us |   3.25 us |  1.00 |    0.01 |
	// |                 Divide |    820.8 us |   4.07 us |   3.81 us |  1.17 |    0.01 |
	// |             Reciprocal |    826.8 us |   4.23 us |   3.95 us |  1.18 |    0.01 |
	// |         ReciprocalSqrt |  2,198.9 us |   6.02 us |   5.34 us |  3.13 |    0.01 |
	// |     EstimateReciprocal |    709.2 us |   3.40 us |   3.01 us |  1.01 |    0.01 |
	// | EstimateReciprocalSqrt |    702.6 us |   3.09 us |   2.58 us |  1.00 |    0.00 |
	// |             Conversion |    718.8 us |   2.76 us |   2.30 us |  1.02 |    0.00 |
	// |            SqrtFloat32 |  1,279.0 us |   1.35 us |   1.13 us |  1.82 |    0.00 |
	// |            SqrtFloat64 |  2,085.2 us |   8.40 us |   7.86 us |  2.97 |    0.01 |
	// |                 Cosine | 11,259.0 us |  91.40 us |  85.50 us | 16.03 |    0.13 |
	// |                   Sine | 11,306.5 us |  83.31 us |  77.93 us | 16.10 |    0.10 |
	// |             SineCosine |  9,199.4 us |  73.90 us |  69.13 us | 13.10 |    0.11 |
	// |              Arccosine | 51,052.6 us | 876.27 us | 819.66 us | 72.70 |    1.27 |
	// |                    Exp |  3,412.7 us |  32.23 us |  30.14 us |  4.86 |    0.05 |
	// |                    Log | 34,397.0 us | 533.83 us | 499.35 us | 48.98 |    0.71 |
	// |                   Log2 | 40,813.7 us | 315.03 us | 294.68 us | 58.12 |    0.44 |
	// |                MathAbs |    708.1 us |   5.60 us |   5.23 us |  1.01 |    0.01 |
	// |            FastMathAbs |    703.3 us |   1.11 us |   0.87 us |  1.00 |    0.00 |
	// |               MathMax0 |  5,337.6 us |  38.27 us |  35.80 us |  7.60 |    0.06 |
	// |           FastMathMax0 |    706.8 us |   3.82 us |   3.57 us |  1.01 |    0.00 |

	[Benchmark(Baseline = true)]
	public float Overhead()
	{
		float result = 0f;

		foreach (float value in array) result += value;

		return result;
	}

	[Benchmark]
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
	public float Divide()
	{
		float result = 0f;

		foreach (float value in array) result += value / value;

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
	public float SqrtFloat32()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Sqrt(value);

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
	public float Cosine()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Cos(value);

		return result;
	}

	[Benchmark]
	public float Sine()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Sin(value);

		return result;
	}

	[Benchmark]
	public float SineCosine()
	{
		float result = 0f;

		foreach (float value in array)
		{
			(float sin, float cos) = MathF.SinCos(value);
			result += sin + cos;
		}

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

	[Benchmark]
	public float MathMax0()
	{
		float result = 0f;

		foreach (float value in array) result += Math.Max(value, 0f);

		return result;
	}

	[Benchmark]
	public float FastMathMax0()
	{
		float result = 0f;

		foreach (float value in array) result += FastMath.Max0(value);

		return result;
	}
}