using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Aggregation.Acceleration;

/// <summary>
/// Stores four <see cref="AxisAlignedBoundingBox"/> in six four-wide SIMD vectors.
/// </summary>
public readonly struct AxisAlignedBoundingBox4
{
	public AxisAlignedBoundingBox4(in AxisAlignedBoundingBox aabb0, in AxisAlignedBoundingBox aabb1, in AxisAlignedBoundingBox aabb2, in AxisAlignedBoundingBox aabb3)
	{
		minX = Make(aabb0.min.x, aabb1.min.x, aabb2.min.x, aabb3.min.x);
		minY = Make(aabb0.min.y, aabb1.min.y, aabb2.min.y, aabb3.min.y);
		minZ = Make(aabb0.min.z, aabb1.min.z, aabb2.min.z, aabb3.min.z);

		maxX = Make(aabb0.max.x, aabb1.max.x, aabb2.max.x, aabb3.max.x);
		maxY = Make(aabb0.max.y, aabb1.max.y, aabb2.max.y, aabb3.max.y);
		maxZ = Make(aabb0.max.z, aabb1.max.z, aabb2.max.z, aabb3.max.z);
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
		Vector128<float> origin = Make(ray.origin.x);
		Vector128<float> inverseDirection = Make(ray.inverseDirection.x);

		Vector128<float> length0 = Sse.Multiply(Sse.Subtract(minX, origin), inverseDirection);
		Vector128<float> length1 = Sse.Multiply(Sse.Subtract(maxX, origin), inverseDirection);

		Vector128<float> far = Sse.Max(length0, length1);
		Vector128<float> near = Sse.Min(length0, length1);

		//Y axis
		origin = Make(ray.origin.y);
		inverseDirection = Make(ray.inverseDirection.y);

		length0 = Sse.Multiply(Sse.Subtract(minY, origin), inverseDirection);
		length1 = Sse.Multiply(Sse.Subtract(maxY, origin), inverseDirection);

		far = Sse.Min(far, Sse.Max(length0, length1));
		near = Sse.Max(near, Sse.Min(length0, length1));

		//Z axis
		origin = Make(ray.origin.z);
		inverseDirection = Make(ray.inverseDirection.z);

		length0 = Sse.Multiply(Sse.Subtract(minZ, origin), inverseDirection);
		length1 = Sse.Multiply(Sse.Subtract(maxZ, origin), inverseDirection);

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
}