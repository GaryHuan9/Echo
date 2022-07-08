using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Experimental.Benchmarks;

public class BoxBounds
{
	public BoxBounds()
	{
		Prng random = new SystemPrng(42);

		bound0 = CreateBound();
		bound1 = CreateBound();
		bound2 = CreateBound();
		bound3 = CreateBound();

		bound0 = new BoxBound(Float3.NegativeOne, Float3.One);
		bound = new BoxBound4(bound0, bound1, bound2, bound3);

		Float3 origin = CreateFloat3(10f);

		ray = new Ray(origin, -origin.Normalized);

		BoxBound CreateBound()
		{
			Float3 point0 = CreateFloat3(3f);
			Float3 point1 = CreateFloat3(3f);

			return new BoxBound(point0.Min(point1), point0.Max(point1));
		}

		Float3 CreateFloat3(float range) => random.Next3(-range, range);
	}

	readonly BoxBound bound0;
	readonly BoxBound bound1;
	readonly BoxBound bound2;
	readonly BoxBound bound3;

	readonly BoxBound4 bound;

	readonly Ray ray;

	// |  Method |      Mean |     Error |    StdDev |
	// |-------- |----------:|----------:|----------:|
	// | Regular | 12.454 ns | 0.0538 ns | 0.0503 ns |
	// |    Quad |  3.144 ns | 0.0388 ns | 0.0363 ns |

	[Benchmark]
	public Vector128<float> Regular() => Vector128.Create
	(
		bound0.Intersect(ray),
		bound1.Intersect(ray),
		bound2.Intersect(ray),
		bound3.Intersect(ray)
	);

	[Benchmark]
	public Float4 Quad() => bound.Intersect(ray);
}