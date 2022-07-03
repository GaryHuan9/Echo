﻿using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Experimental.Benchmarks;

public class AabbSimd
{
	public AabbSimd()
	{
		Prng random = new SystemPrng(42);

		aabb0 = CreateAABB();
		aabb1 = CreateAABB();
		aabb2 = CreateAABB();
		aabb3 = CreateAABB();

		aabb0 = new AxisAlignedBoundingBox(Float3.NegativeOne, Float3.One);
		aabb = new AxisAlignedBoundingBox4(aabb0, aabb1, aabb2, aabb3);

		Float3 origin = CreateFloat3(10f);

		ray = new Ray(origin, -origin.Normalized);

		AxisAlignedBoundingBox CreateAABB()
		{
			Float3 point0 = CreateFloat3(3f);
			Float3 point1 = CreateFloat3(3f);

			return new AxisAlignedBoundingBox(point0.Min(point1), point0.Max(point1));
		}

		Float3 CreateFloat3(float range) => random.Next3(-range, range);
	}

	readonly AxisAlignedBoundingBox aabb0;
	readonly AxisAlignedBoundingBox aabb1;
	readonly AxisAlignedBoundingBox aabb2;
	readonly AxisAlignedBoundingBox aabb3;

	readonly AxisAlignedBoundingBox4 aabb;

	readonly Ray ray;

	// |  Method |      Mean |     Error |    StdDev |
	// |-------- |----------:|----------:|----------:|
	// | Regular | 12.454 ns | 0.0538 ns | 0.0503 ns |
	// |    Quad |  3.144 ns | 0.0388 ns | 0.0363 ns |

	[Benchmark]
	public Vector128<float> Regular() => Vector128.Create
	(
		aabb0.Intersect(ray),
		aabb1.Intersect(ray),
		aabb2.Intersect(ray),
		aabb3.Intersect(ray)
	);

	[Benchmark]
	public Float4 Quad() => aabb.Intersect(ray);
}