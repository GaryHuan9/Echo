using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Mathematics.Primitives;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// Stores four <see cref="AxisAlignedBoundingBox"/> in six four-wide SIMD vectors.
/// </summary>
public readonly struct AxisAlignedBoundingBox4
{
	public AxisAlignedBoundingBox4(in AxisAlignedBoundingBox aabb0, in AxisAlignedBoundingBox aabb1, in AxisAlignedBoundingBox aabb2, in AxisAlignedBoundingBox aabb3)
	{
		minX = Make(aabb0.min.X, aabb1.min.X, aabb2.min.X, aabb3.min.X);
		minY = Make(aabb0.min.Y, aabb1.min.Y, aabb2.min.Y, aabb3.min.Y);
		minZ = Make(aabb0.min.Z, aabb1.min.Z, aabb2.min.Z, aabb3.min.Z);

		maxX = Make(aabb0.max.X, aabb1.max.X, aabb2.max.X, aabb3.max.X);
		maxY = Make(aabb0.max.Y, aabb1.max.Y, aabb2.max.Y, aabb3.max.Y);
		maxZ = Make(aabb0.max.Z, aabb1.max.Z, aabb2.max.Z, aabb3.max.Z);
	}

	public AxisAlignedBoundingBox4(ReadOnlySpan<AxisAlignedBoundingBox> aabbs) : this
	(
		aabbs.TryGetValue(0, AxisAlignedBoundingBox.none), aabbs.TryGetValue(1, AxisAlignedBoundingBox.none),
		aabbs.TryGetValue(2, AxisAlignedBoundingBox.none), aabbs.TryGetValue(3, AxisAlignedBoundingBox.none)
	) { }

	readonly Vector128<float> minX;
	readonly Vector128<float> minY;
	readonly Vector128<float> minZ;

	readonly Vector128<float> maxX;
	readonly Vector128<float> maxY;
	readonly Vector128<float> maxZ;

	/// <summary>
	/// Extracts out and returns a particular <see cref="AxisAlignedBoundingBox"/> at
	/// <paramref name="index"/>, which should be between 0 (inclusive) and 4 (exclusive).
	/// </summary>
	public AxisAlignedBoundingBox this[int index] => new
	(
		new Float3(minX.GetElement(index), minY.GetElement(index), minZ.GetElement(index)),
		new Float3(maxX.GetElement(index), maxY.GetElement(index), maxZ.GetElement(index))
	);

	/// <summary>
	/// Calculates and returns an <see cref="AxisAlignedBoundingBox"/> that encapsulates the four
	/// <see cref="AxisAlignedBoundingBox"/> contained in this <see cref="AxisAlignedBoundingBox4"/>.
	/// </summary>
	public AxisAlignedBoundingBox Encapsulated => new
	(
		new Float3(Utilities.ToFloat4(minX).MinComponent, Utilities.ToFloat4(minY).MinComponent, Utilities.ToFloat4(minZ).MinComponent),
		new Float3(Utilities.ToFloat4(maxX).MaxComponent, Utilities.ToFloat4(maxY).MaxComponent, Utilities.ToFloat4(maxZ).MaxComponent)
	);

	/// <summary>
	/// Finds the intersection between <paramref name="ray"/> and this <see cref="AxisAlignedBoundingBox4"/>.
	/// Returns either the intersection distance or <see cref="float.PositiveInfinity"/> if none was found.
	/// </summary>
	public Vector128<float> Intersect(in Ray ray)
	{
		//X axis
		Vector128<float> origin = Make(ray.origin.X);
		Vector128<float> directionR = Make(ray.directionR.X);

		Vector128<float> length0 = Sse.Multiply(Sse.Subtract(minX, origin), directionR);
		Vector128<float> length1 = Sse.Multiply(Sse.Subtract(maxX, origin), directionR);

		Vector128<float> far = Sse.Max(length0, length1);
		Vector128<float> near = Sse.Min(length0, length1);

		//Y axis
		origin = Make(ray.origin.Y);
		directionR = Make(ray.directionR.Y);

		length0 = Sse.Multiply(Sse.Subtract(minY, origin), directionR);
		length1 = Sse.Multiply(Sse.Subtract(maxY, origin), directionR);

		far = Sse.Min(far, Sse.Max(length0, length1));
		near = Sse.Max(near, Sse.Min(length0, length1));

		//Z axis
		origin = Make(ray.origin.Z);
		directionR = Make(ray.directionR.Z);

		length0 = Sse.Multiply(Sse.Subtract(minZ, origin), directionR);
		length1 = Sse.Multiply(Sse.Subtract(maxZ, origin), directionR);

		far = Sse.Min(far, Sse.Max(length0, length1));
		near = Sse.Max(near, Sse.Min(length0, length1));

		far = Sse.Multiply(far, Make(AxisAlignedBoundingBox.FarMultiplier));

		//See the single AABB implementation about the order of the positive infinity vs near
		return Sse41.BlendVariable
		(
			Make(float.PositiveInfinity), near, Sse.And
			(
				Sse.CompareGreaterThanOrEqual(far, near),
				Sse.CompareGreaterThanOrEqual(far, Vector128<float>.Zero)
			)
		);
	}

	public override unsafe int GetHashCode()
	{
		fixed (AxisAlignedBoundingBox4* ptr = &this) return Utilities.GetHashCode(ptr);
	}

	static Vector128<float> Make(float value) => Vector128.Create(value);

	static Vector128<float> Make(float value0, float value1, float value2, float value3) => Vector128.Create(value0, value1, value2, value3);

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