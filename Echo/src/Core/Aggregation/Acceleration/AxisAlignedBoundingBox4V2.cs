using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Mathematics.Primitives;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// Stores four <see cref="AxisAlignedBoundingBox"/> in six four-wide SIMD vectors.
/// </summary>
public readonly struct AxisAlignedBoundingBox4V2
{
	public AxisAlignedBoundingBox4V2(in AxisAlignedBoundingBox aabb0, in AxisAlignedBoundingBox aabb1, in AxisAlignedBoundingBox aabb2, in AxisAlignedBoundingBox aabb3)
	{
		minMaxX = Vector256.Create(aabb0.min.X, aabb1.min.X, aabb0.max.X, aabb1.max.X, aabb2.min.X, aabb3.min.X, aabb2.max.X, aabb3.max.X);
		minMaxY = Vector256.Create(aabb0.min.Y, aabb1.min.Y, aabb0.max.Y, aabb1.max.Y, aabb2.min.Y, aabb3.min.Y, aabb2.max.Y, aabb3.max.Y);
		minMaxZ = Vector256.Create(aabb0.min.Z, aabb1.min.Z, aabb0.max.Z, aabb1.max.Z, aabb2.min.Z, aabb3.min.Z, aabb2.max.Z, aabb3.max.Z);
	}

	public AxisAlignedBoundingBox4V2(ReadOnlySpan<AxisAlignedBoundingBox> aabbs) : this
	(
		aabbs.TryGetValue(0, AxisAlignedBoundingBox.none), aabbs.TryGetValue(1, AxisAlignedBoundingBox.none),
		aabbs.TryGetValue(2, AxisAlignedBoundingBox.none), aabbs.TryGetValue(3, AxisAlignedBoundingBox.none)
	) { }

	readonly Vector256<float> minMaxX;
	readonly Vector256<float> minMaxY;
	readonly Vector256<float> minMaxZ;

	/// <summary>
	/// Finds the intersection between <paramref name="ray"/> and this <see cref="AxisAlignedBoundingBox4"/>.
	/// Returns either the intersection distance or <see cref="float.PositiveInfinity"/> if none was found.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public Vector128<float> Intersect(in Ray ray)
	{
		//X axis
		Vector256<float> origin = Vector256.Create(ray.origin.X);
		Vector256<float> directionR = Vector256.Create(ray.directionR.X);

		// Vector256<float> sign = Avx.CompareGreaterThan(directionR, Vector256<float>.Zero);
		Vector256<long> sign = Avx2.ShiftRightLogical(directionR.AsInt32(), 30).AsInt64();
		sign = Avx2.Xor(sign, Vector256.Create(0L, 2L, 0L, 2L));

		Vector256<float> length = Avx.Multiply(Avx.Subtract(minMaxX, origin), directionR);
		length = Avx.PermuteVar(length.AsDouble(), sign).AsSingle(); //min0, min1, max0, max1, ...
		Vector256<float> nearFar = length;                           //near0, near1, far0, far1, ...

		//Y axis
		origin = Vector256.Create(ray.origin.Y);
		directionR = Vector256.Create(ray.directionR.Y);

		sign = Avx2.ShiftRightLogical(directionR.AsInt32(), 30).AsInt64();
		sign = Avx2.Xor(sign, Vector256.Create(0L, 2L, 0L, 2L));

		length = Avx.Multiply(Avx.Subtract(minMaxY, origin), directionR);
		length = Avx.PermuteVar(length.AsDouble(), sign).AsSingle(); //min0, min1, max0, max1, ...

		Vector256<float> min = Avx.Min(length, nearFar);
		Vector256<float> max = Avx.Max(length, nearFar);

		nearFar = Avx.Shuffle(max, min, 0b1110_0100);

		//Z axis
		origin = Vector256.Create(ray.origin.Z);
		directionR = Vector256.Create(ray.directionR.Z);

		sign = Avx2.ShiftRightLogical(directionR.AsInt32(), 30).AsInt64();
		sign = Avx2.Xor(sign, Vector256.Create(0L, 2L, 0L, 2L));

		length = Avx.Multiply(Avx.Subtract(minMaxZ, origin), directionR);
		length = Avx.PermuteVar(length.AsDouble(), sign).AsSingle(); //min0, min1, max0, max1, ...

		min = Avx.Min(length, nearFar);
		max = Avx.Max(length, nearFar);

		nearFar = Avx.Shuffle(max, min, 0b1110_0100);

		Vector256<float> near = Avx.Permute(nearFar, 0b0100_1110);
		Vector256<float> far = Avx.Multiply(nearFar, Vector256.Create(AxisAlignedBoundingBox.FarMultiplier));

		Vector256<float> result = Avx.BlendVariable
		(
			Vector256.Create(float.PositiveInfinity), near, Avx.And
			(
				Avx.CompareGreaterThanOrEqual(far, near),
				Avx.CompareGreaterThanOrEqual(far, Vector256<float>.Zero)
			)
		);

		return Avx2.PermuteVar8x32(result, Vector256.Create(2, 3, 6, 7, 0, 1, 4, 5)).GetLower();
	}
}