using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Memory;

namespace Echo.Experimental.Benchmarks;

public class DotProduct
{
	public DotProduct()
	{
		Prng random = new SystemPrng(42);

		array0 = CreateArray();
		array1 = CreateArray();

		aligned0 = new AlignedArray<float>(array0);
		aligned1 = new AlignedArray<float>(array1);

		foreach ((MethodInfo method, float result) in from method in typeof(DotProduct).GetMethods()
													  where method.GetCustomAttribute<BenchmarkAttribute>() != null
													  select (method, (float)method.Invoke(this, null)!))
		{
			DebugHelper.Log(method.Name, result);
		}

		float[] CreateArray()
		{
			var array = new float[TotalLength];

			for (int i = 0; i < TotalLength; i++) array[i] = random.Next1();

			return array;
		}
	}

	const int TotalLength = 300000 * 8 * 4;

	readonly float[] array0;
	readonly float[] array1;

	readonly AlignedArray<float> aligned0;
	readonly AlignedArray<float> aligned1;

	// |             Method |     Mean |     Error |    StdDev |
	// |------------------- |---------:|----------:|----------:|
	// |         NormalLoop | 7.028 ms | 0.0494 ms | 0.0463 ms |
	// |       NormalUnroll | 5.069 ms | 0.0185 ms | 0.0173 ms |
	// | NormalUnrollBounds | 4.945 ms | 0.0494 ms | 0.0412 ms |
	// |  NormalUnrollSmall | 5.262 ms | 0.0058 ms | 0.0052 ms |
	// |    NormalUnrollBig | 4.899 ms | 0.0289 ms | 0.0270 ms |
	// |            AvxLoop | 2.273 ms | 0.0060 ms | 0.0046 ms |
	// |            SseLoop | 3.024 ms | 0.0321 ms | 0.0300 ms |
	// |          AvxUnroll | 2.252 ms | 0.0159 ms | 0.0148 ms |
	// |         AvxAligned | 2.175 ms | 0.0232 ms | 0.0217 ms |
	// |   AvxUnrollAligned | 2.202 ms | 0.0202 ms | 0.0189 ms |

	[Benchmark]
	public float NormalLoop()
	{
		float result = 0f;

		for (int i = 0; i < TotalLength; i++) result += array0[i] * array1[i];

		return result;
	}

	[Benchmark]
	public float NormalUnroll()
	{
		float result0 = 0f;
		float result1 = 0f;
		float result2 = 0f;
		float result3 = 0f;

		for (int i = 0; i < TotalLength; i += 4)
		{
			result0 += array0[i + 0] * array1[i + 0];
			result1 += array0[i + 1] * array1[i + 1];
			result2 += array0[i + 2] * array1[i + 2];
			result3 += array0[i + 3] * array1[i + 3];
		}

		return result0 + result1 + result2 + result3;
	}

	[Benchmark]
	public float NormalUnrollBounds()
	{
		float result0 = 0f;
		float result1 = 0f;
		float result2 = 0f;
		float result3 = 0f;

		for (int i = 0; i < TotalLength; i += 4)
		{
			result3 += array0[i + 3] * array1[i + 3];
			result2 += array0[i + 2] * array1[i + 2];
			result1 += array0[i + 1] * array1[i + 1];
			result0 += array0[i + 0] * array1[i + 0];
		}

		return result0 + result1 + result2 + result3;
	}

	[Benchmark]
	public float NormalUnrollSmall()
	{
		float result0 = 0f;
		float result1 = 0f;

		for (int i = 0; i < TotalLength; i += 2)
		{
			result0 += array0[i + 0] * array1[i + 0];
			result1 += array0[i + 1] * array1[i + 1];
		}

		return result0 + result1;
	}

	[Benchmark]
	public float NormalUnrollBig()
	{
		float result0 = 0f;
		float result1 = 0f;
		float result2 = 0f;
		float result3 = 0f;
		float result4 = 0f;
		float result5 = 0f;
		float result6 = 0f;
		float result7 = 0f;

		for (int i = 0; i < TotalLength; i += 8)
		{
			result0 += array0[i + 0] * array1[i + 0];
			result1 += array0[i + 1] * array1[i + 1];
			result2 += array0[i + 2] * array1[i + 2];
			result3 += array0[i + 3] * array1[i + 3];
			result4 += array0[i + 4] * array1[i + 4];
			result5 += array0[i + 5] * array1[i + 5];
			result6 += array0[i + 6] * array1[i + 6];
			result7 += array0[i + 7] * array1[i + 7];
		}

		return result0 + result1 + result2 + result3 + result4 + result5 + result6 + result7;
	}

	[Benchmark]
	public unsafe float AvxLoop()
	{
		Vector256<float> result = Vector256<float>.Zero;

		fixed (float* pointer0 = array0)
		fixed (float* pointer1 = array1)
		{
			for (int i = 0; i < TotalLength; i += 8)
			{
				Vector256<float> vector0 = Avx.LoadVector256(pointer0 + i);
				Vector256<float> vector1 = Avx.LoadVector256(pointer1 + i);

				result = Fma.MultiplyAdd(vector0, vector1, result);
			}
		}

		Vector128<float> narrow = Sse.Add(result.GetLower(), result.GetUpper());
		narrow = Sse3.HorizontalAdd(narrow, narrow);
		narrow = Sse3.HorizontalAdd(narrow, narrow);
		return narrow.ToScalar();
	}

	[Benchmark]
	public unsafe float SseLoop()
	{
		Vector128<float> result = Vector128<float>.Zero;

		fixed (float* pointer0 = array0)
		fixed (float* pointer1 = array1)
		{
			for (int i = 0; i < TotalLength; i += 4)
			{
				Vector128<float> vector0 = Sse.LoadVector128(pointer0 + i);
				Vector128<float> vector1 = Sse.LoadVector128(pointer1 + i);

				result = Fma.MultiplyAdd(vector0, vector1, result);
			}
		}

		result = Sse3.HorizontalAdd(result, result);
		result = Sse3.HorizontalAdd(result, result);
		return result.ToScalar();
	}

	[Benchmark]
	public unsafe float AvxUnroll()
	{
		Vector256<float> result0 = Vector256<float>.Zero;
		Vector256<float> result1 = Vector256<float>.Zero;
		Vector256<float> result2 = Vector256<float>.Zero;
		Vector256<float> result3 = Vector256<float>.Zero;

		fixed (float* pointer0 = array0)
		fixed (float* pointer1 = array1)
		{
			for (int i = 0; i < TotalLength; i += 8 * 4)
			{
				float* offset0 = pointer0 + i;
				float* offset1 = pointer1 + i;

				result0 = Fma.MultiplyAdd(Avx.LoadVector256(offset0 + 0 * 8), Avx.LoadVector256(offset1 + 0 * 8), result0);
				result1 = Fma.MultiplyAdd(Avx.LoadVector256(offset0 + 1 * 8), Avx.LoadVector256(offset1 + 1 * 8), result1);
				result2 = Fma.MultiplyAdd(Avx.LoadVector256(offset0 + 2 * 8), Avx.LoadVector256(offset1 + 2 * 8), result2);
				result3 = Fma.MultiplyAdd(Avx.LoadVector256(offset0 + 3 * 8), Avx.LoadVector256(offset1 + 3 * 8), result3);
			}
		}

		Vector256<float> result = Avx.Add(Avx.Add(result0, result1), Avx.Add(result2, result3));
		Vector128<float> narrow = Sse.Add(result.GetLower(), result.GetUpper());

		narrow = Sse3.HorizontalAdd(narrow, narrow);
		narrow = Sse3.HorizontalAdd(narrow, narrow);
		return narrow.ToScalar();
	}

	[Benchmark]
	public unsafe float AvxAligned()
	{
		Vector256<float> result = Vector256<float>.Zero;

		float* pointer0 = aligned0.Pointer;
		float* pointer1 = aligned1.Pointer;

		for (int i = 0; i < TotalLength; i += 8)
		{
			result = Fma.MultiplyAdd(Avx.LoadAlignedVector256(pointer0), Avx.LoadAlignedVector256(pointer1), result);

			pointer0 += 8;
			pointer1 += 8;
		}

		Vector128<float> narrow = Sse.Add(result.GetLower(), result.GetUpper());

		narrow = Sse3.HorizontalAdd(narrow, narrow);
		narrow = Sse3.HorizontalAdd(narrow, narrow);
		return narrow.ToScalar();
	}

	[Benchmark]
	public unsafe float AvxUnrollAligned()
	{
		Vector256<float> result0 = Vector256<float>.Zero;
		Vector256<float> result1 = Vector256<float>.Zero;
		Vector256<float> result2 = Vector256<float>.Zero;
		Vector256<float> result3 = Vector256<float>.Zero;

		float* pointer0 = aligned0.Pointer;
		float* pointer1 = aligned1.Pointer;

		for (int i = 0; i < TotalLength; i += 8 * 4)
		{
			result0 = Fma.MultiplyAdd(Avx.LoadAlignedVector256(pointer0 + 0 * 8), Avx.LoadAlignedVector256(pointer1 + 0 * 8), result0);
			result1 = Fma.MultiplyAdd(Avx.LoadAlignedVector256(pointer0 + 1 * 8), Avx.LoadAlignedVector256(pointer1 + 1 * 8), result1);
			result2 = Fma.MultiplyAdd(Avx.LoadAlignedVector256(pointer0 + 2 * 8), Avx.LoadAlignedVector256(pointer1 + 2 * 8), result2);
			result3 = Fma.MultiplyAdd(Avx.LoadAlignedVector256(pointer0 + 3 * 8), Avx.LoadAlignedVector256(pointer1 + 3 * 8), result3);

			pointer0 += 8 * 4;
			pointer1 += 8 * 4;
		}

		Vector256<float> result = Avx.Add(Avx.Add(result0, result1), Avx.Add(result2, result3));
		Vector128<float> narrow = Sse.Add(result.GetLower(), result.GetUpper());

		narrow = Sse3.HorizontalAdd(narrow, narrow);
		narrow = Sse3.HorizontalAdd(narrow, narrow);
		return narrow.ToScalar();
	}
}