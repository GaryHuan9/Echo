using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;

namespace Echo.Core.Aggregation.Bounds;

/// <summary>
/// Stores four <see cref="AxisAlignedBoundingBox"/> in six four-wide SIMD vectors.
/// </summary>
public readonly struct AxisAlignedBoundingBox4
{
	public AxisAlignedBoundingBox4(in AxisAlignedBoundingBox aabb0, in AxisAlignedBoundingBox aabb1, in AxisAlignedBoundingBox aabb2, in AxisAlignedBoundingBox aabb3)
	{
		minX = new Float4(aabb0.min.X, aabb1.min.X, aabb2.min.X, aabb3.min.X);
		minY = new Float4(aabb0.min.Y, aabb1.min.Y, aabb2.min.Y, aabb3.min.Y);
		minZ = new Float4(aabb0.min.Z, aabb1.min.Z, aabb2.min.Z, aabb3.min.Z);

		maxX = new Float4(aabb0.max.X, aabb1.max.X, aabb2.max.X, aabb3.max.X);
		maxY = new Float4(aabb0.max.Y, aabb1.max.Y, aabb2.max.Y, aabb3.max.Y);
		maxZ = new Float4(aabb0.max.Z, aabb1.max.Z, aabb2.max.Z, aabb3.max.Z);
	}

	public AxisAlignedBoundingBox4(ReadOnlySpan<AxisAlignedBoundingBox> aabbs) : this
	(
		aabbs.TryGetValue(0, AxisAlignedBoundingBox.none), aabbs.TryGetValue(1, AxisAlignedBoundingBox.none),
		aabbs.TryGetValue(2, AxisAlignedBoundingBox.none), aabbs.TryGetValue(3, AxisAlignedBoundingBox.none)
	) { }

	readonly Float4 minX;
	readonly Float4 minY;
	readonly Float4 minZ;

	readonly Float4 maxX;
	readonly Float4 maxY;
	readonly Float4 maxZ;

	/// <summary>
	/// Extracts out and returns a particular <see cref="AxisAlignedBoundingBox"/> at
	/// <paramref name="index"/>, which should be between 0 (inclusive) and 4 (exclusive).
	/// </summary>
	public AxisAlignedBoundingBox this[int index] => new
	(
		new Float3(minX[index], minY[index], minZ[index]),
		new Float3(maxX[index], maxY[index], maxZ[index])
	);

	/// <summary>
	/// Calculates and returns an <see cref="AxisAlignedBoundingBox"/> that encapsulates the four
	/// <see cref="AxisAlignedBoundingBox"/> contained in this <see cref="AxisAlignedBoundingBox4"/>.
	/// </summary>
	public AxisAlignedBoundingBox Encapsulated => new
	(
		new Float3(minX.MinComponent, minY.MinComponent, minZ.MinComponent),
		new Float3(maxX.MaxComponent, maxY.MaxComponent, maxZ.MaxComponent)
	);

	/// <summary>
	/// Finds the intersection between <paramref name="ray"/> and this <see cref="AxisAlignedBoundingBox4"/>.
	/// Returns either the intersection distance or <see cref="float.PositiveInfinity"/> if none was found.
	/// </summary>
	public Float4 Intersect(in Ray ray)
	{
		//X axis
		Float4 origin = (Float4)ray.origin.X;
		Float4 directionR = (Float4)ray.directionR.X;

		//NOTE
		//For some reason the following subtraction generates a weird lea instruction before the actual vmovupd
		//This can be avoided if we replace all of the Float4 in this struct with Vector128<float>, but there is
		//basically no performance difference. Additionally, note that the JIT ASM is significantly larger if
		//the fields in Ray are Float4 rather than Float3, which is super weird.

		Float4 length0 = (minX - origin) * directionR;
		Float4 length1 = (maxX - origin) * directionR;

		Float4 far = length0.Max(length1);
		Float4 near = length0.Min(length1);

		//Y axis
		origin = (Float4)ray.origin.Y;
		directionR = (Float4)ray.directionR.Y;

		length0 = (minY - origin) * directionR;
		length1 = (maxY - origin) * directionR;

		far = far.Min(length0.Max(length1));
		near = near.Max(length0.Min(length1));

		//Z axis
		origin = (Float4)ray.origin.Z;
		directionR = (Float4)ray.directionR.Z;

		length0 = (minZ - origin) * directionR;
		length1 = (maxZ - origin) * directionR;

		far = far.Min(length0.Max(length1));
		near = near.Max(length0.Min(length1));

		far *= AxisAlignedBoundingBox.FarMultiplier;

		return new Float4(Sse41.BlendVariable
		(
			Vector128.Create(float.PositiveInfinity), near.v, Sse.And
			(
				Sse.CompareGreaterThanOrEqual(far.v, near.v),
				Sse.CompareGreaterThanOrEqual(far.v, Vector128<float>.Zero)
			)
		));
	}

	public override unsafe int GetHashCode()
	{
		fixed (AxisAlignedBoundingBox4* ptr = &this) return Utility.GetHashCode(ptr);
	}

	public readonly struct V2
	{
		public V2(in AxisAlignedBoundingBox aabb0, in AxisAlignedBoundingBox aabb1, in AxisAlignedBoundingBox aabb2, in AxisAlignedBoundingBox aabb3)
		{
			minMaxX = Vector256.Create(aabb0.min.X, aabb1.min.X, aabb0.max.X, aabb1.max.X, aabb2.min.X, aabb3.min.X, aabb2.max.X, aabb3.max.X);
			minMaxY = Vector256.Create(aabb0.min.Y, aabb1.min.Y, aabb0.max.Y, aabb1.max.Y, aabb2.min.Y, aabb3.min.Y, aabb2.max.Y, aabb3.max.Y);
			minMaxZ = Vector256.Create(aabb0.min.Z, aabb1.min.Z, aabb0.max.Z, aabb1.max.Z, aabb2.min.Z, aabb3.min.Z, aabb2.max.Z, aabb3.max.Z);
		}

		readonly Vector256<float> minMaxX;
		readonly Vector256<float> minMaxY;
		readonly Vector256<float> minMaxZ;

		/// <summary>
		/// Finds the intersection between <paramref name="ray"/> and this <see cref="AxisAlignedBoundingBox4"/>.
		/// Returns either the intersection distance or <see cref="float.PositiveInfinity"/> if none was found.
		/// </summary>
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
}