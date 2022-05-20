using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Compute;

namespace Echo.Experimental.Benchmarks;

public class BufferCopy
{
	const int Length = 6000000;
	const int Height = 2000;
	const int Width = Length / Height;

	readonly Float4[] source = new Float4[Length];
	readonly Float4[] target = new Float4[Length];

	// |        Method |     Mean |     Error |    StdDev |
	// |-------------- |---------:|----------:|----------:|
	// |      SpanCopy | 4.721 ms | 0.0482 ms | 0.0428 ms |
	// |     ArrayCopy | 4.709 ms | 0.0560 ms | 0.0468 ms |
	// |    MemoryCopy | 4.697 ms | 0.0490 ms | 0.0459 ms |
	// | ParallelNaive | 7.550 ms | 0.0421 ms | 0.0394 ms |
	// |   ParallelRow | 7.431 ms | 0.0244 ms | 0.0228 ms |
	// |  ParallelCore | 4.448 ms | 0.0232 ms | 0.0217 ms |

	// [Benchmark]
	public void SpanCopy() => source.AsSpan(0, Length).CopyTo(target);

	// [Benchmark]
	public void ArrayCopy() => Array.Copy(source, target, Length);

	// [Benchmark]
	public unsafe void MemoryCopy()
	{
		fixed (Float4* s = source)
		fixed (Float4* t = target)
		{
			MemoryCopy(s, t, Length);
		}
	}

	// [Benchmark]
	public void ParallelNaive()
	{
		Parallel.For(0, Length, i => target[i] = source[i]);
	}

	// [Benchmark]
	public unsafe void ParallelRow()
	{
		fixed (Float4* s = source)
		fixed (Float4* t = target)
		{
			nuint ptr0 = (nuint)s;
			nuint ptr1 = (nuint)t;

			Parallel.For(0, Height, i =>
			{
				int shift = i * Width;
				MemoryCopy((Float4*)ptr0 + shift, (Float4*)ptr1 + shift, Width);
			});
		}
	}

	[Benchmark]
	public unsafe void ParallelCore()
	{
		int count = Environment.ProcessorCount;
		int stride = Length / count;

		fixed (Float4* s = source)
		fixed (Float4* t = target)
		{
			nuint ptr0 = (nuint)s;
			nuint ptr1 = (nuint)t;

			Parallel.For(0, count, i =>
			{
				int shift = i * stride;
				MemoryCopy((Float4*)ptr0 + shift, (Float4*)ptr1 + shift, stride);
			});
		}
	}

	static unsafe void MemoryCopy<T>(T* source, T* target, int length) where T : unmanaged
	{
		Buffer.MemoryCopy(source, target, length * sizeof(Float4), length * sizeof(Float4));
	}
}