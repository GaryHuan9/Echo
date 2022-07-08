using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;

namespace Echo.Core.Aggregation.Bounds;

/// <summary>
/// Stores four <see cref="BoxBound"/> in six four-wide SIMD vectors.
/// </summary>
public readonly struct BoxBound4
{
	public BoxBound4(in BoxBound bound0, in BoxBound bound1, in BoxBound bound2, in BoxBound bound3)
	{
		minX = new Float4(bound0.min.X, bound1.min.X, bound2.min.X, bound3.min.X);
		minY = new Float4(bound0.min.Y, bound1.min.Y, bound2.min.Y, bound3.min.Y);
		minZ = new Float4(bound0.min.Z, bound1.min.Z, bound2.min.Z, bound3.min.Z);

		maxX = new Float4(bound0.max.X, bound1.max.X, bound2.max.X, bound3.max.X);
		maxY = new Float4(bound0.max.Y, bound1.max.Y, bound2.max.Y, bound3.max.Y);
		maxZ = new Float4(bound0.max.Z, bound1.max.Z, bound2.max.Z, bound3.max.Z);
	}

	public BoxBound4(ReadOnlySpan<BoxBound> bounds) : this
	(
		bounds.TryGetValue(0, BoxBound.None), bounds.TryGetValue(1, BoxBound.None),
		bounds.TryGetValue(2, BoxBound.None), bounds.TryGetValue(3, BoxBound.None)
	) { }

	readonly Float4 minX;
	readonly Float4 minY;
	readonly Float4 minZ;

	readonly Float4 maxX;
	readonly Float4 maxY;
	readonly Float4 maxZ;

	/// <summary>
	/// Extracts out and returns a particular <see cref="BoxBound"/> at
	/// <paramref name="index"/>, which should be between 0 (inclusive) and 4 (exclusive).
	/// </summary>
	public BoxBound this[int index] => new
	(
		new Float3(minX[index], minY[index], minZ[index]),
		new Float3(maxX[index], maxY[index], maxZ[index])
	);

	/// <summary>
	/// Calculates and returns an <see cref="BoxBound"/> that encapsulates the four
	/// <see cref="BoxBound"/> contained in this <see cref="BoxBound4"/>.
	/// </summary>
	public BoxBound Encapsulated => new
	(
		new Float3(minX.MinComponent, minY.MinComponent, minZ.MinComponent),
		new Float3(maxX.MaxComponent, maxY.MaxComponent, maxZ.MaxComponent)
	);

	/// <summary>
	/// Finds the intersection between <paramref name="ray"/> and this <see cref="BoxBound4"/>.
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

		far *= BoxBound.FarMultiplier;

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
		fixed (BoxBound4* ptr = &this) return Utility.GetHashCode(ptr);
	}
}