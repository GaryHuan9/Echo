using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Experimental;

[SimpleJob(RuntimeMoniker.Net60)]
public class BenchmarkMath
{
	public BenchmarkMath()
	{
		array = new float[1024 * 1024];
		Random random = new Random(42);

		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (float)(random.NextDouble() * 100f);
		}
	}

	readonly float[] array;

	// Input range 0 - 100
	// | Method |        Mean |       Error |      StdDev |
	// |------- |------------:|------------:|------------:|
	// |  SqrtD |  2,072.1 us |     3.59 us |     3.36 us |
	// |  SqrtF |  1,289.8 us |     8.73 us |     8.17 us |
	// |    Sin | 11,295.2 us |    75.03 us |    70.18 us |
	// |    Tan | 10,597.0 us |    73.74 us |    68.97 us |
	// |   Acos | 52,618.7 us | 1,044.78 us | 1,072.92 us |
	// |    Mul |    705.2 us |     5.46 us |     5.10 us |
	// |    Exp | 11,148.5 us |   113.32 us |   106.00 us |
	// |    Log |  4,085.6 us |    28.68 us |    26.83 us |
	// |   Log2 | 26,367.8 us |   181.86 us |   170.11 us |

	// Input range 0 - 1
	// | Method |        Mean |    Error |   StdDev |
	// |------- |------------:|---------:|---------:|
	// |  SqrtD |  2,079.1 us |  6.56 us |  6.13 us |
	// |  SqrtF |  1,283.2 us |  3.81 us |  3.57 us |
	// |    Sin |  5,826.2 us | 36.49 us | 34.14 us |
	// |    Tan |  6,013.7 us | 39.47 us | 36.92 us |
	// |   Acos |  9,709.5 us | 59.30 us | 55.47 us |
	// |    Mul |    703.2 us |  2.23 us |  2.09 us |
	// |    Exp |  3,324.3 us | 14.16 us | 12.55 us |
	// |    Log |  4,661.8 us | 15.97 us | 14.16 us |
	// |   Log2 | 25,591.1 us | 93.00 us | 86.99 us |

	// Input range 0 - 100
	// |          Method |       Mean |    Error |  StdDev | Ratio | RatioSD |
	// |---------------- |-----------:|---------:|--------:|------:|--------:|
	// |        Overhead |   697.2 us |  2.46 us | 2.30 us |  1.00 |    0.00 |
	// |        Multiply |   698.7 us |  2.51 us | 2.35 us |  1.00 |    0.00 |
	// |      Reciprocal |   817.6 us |  4.49 us | 3.98 us |  1.17 |    0.01 |
	// |     InverseSqrt | 2,180.7 us | 10.13 us | 9.48 us |  3.12 |    0.02 |
	// | FastInverseSqrt | 2,136.8 us |  8.59 us | 8.03 us |  3.06 |    0.01 |
	// |           SqrtF | 1,273.2 us |  6.20 us | 5.80 us |  1.82 |    0.01 |

	[Benchmark]
	public float Overhead()
	{
		float result = 0f;

		foreach (float value in array) result += value;

		return result;
	}

	[Benchmark(Baseline = true)]
	public float Multiply()
	{
		float result = 0f;

		//NOTE: Multiply takes about the same amount of time as Overhead because of FMA! (I think)

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
	public float EstimateReciprocal()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.ReciprocalEstimate(value);

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
	public float FastReciprocalSqrt()
	{
		float result = 0f;

		foreach (float value in array) result += FRSqrt(value);

		return result;

		static float FRSqrt(float value)
		{
			float x2 = value * 0.5F;
			int i = Scalars.SingleToInt32Bits(value);
			i = 0x5F3759DF - (i >> 1);
			float y = Scalars.Int32ToSingleBits(i);
			y *= 1.5f - x2 * y * y;
			y *= 1.5f - x2 * y * y;

			return y;
		}
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
	public float SqrtD()
	{
		float result = 0f;

		foreach (float value in array) result += (float)Math.Sqrt(value);

		return result;
	}

	[Benchmark]
	public float SqrtF()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Sqrt(value);

		return result;
	}

	[Benchmark]
	public float Sin()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Sin(value);

		return result;
	}

	[Benchmark]
	public float Tan()
	{
		float result = 0f;

		foreach (float value in array) result += MathF.Tan(value);

		return result;
	}

	[Benchmark]
	public float Acos()
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
}