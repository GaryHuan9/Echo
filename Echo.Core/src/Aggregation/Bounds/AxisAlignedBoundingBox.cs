using System;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;

namespace Echo.Core.Aggregation.Bounds;

/// <summary>
/// A 3D box that is aligned to the coordinate axes, usually used to bound other objects.
/// </summary>
public readonly struct AxisAlignedBoundingBox : IFormattable
{
	public AxisAlignedBoundingBox(in Float3 min, in Float3 max)
	{
		this.min = min;
		this.max = max;

		Assert.IsTrue(max >= min);
	}

	public AxisAlignedBoundingBox(ReadOnlySpan<Float3> points)
	{
		Assert.AreNotEqual(points.Length, 0);

		min = Float3.PositiveInfinity;
		max = Float3.NegativeInfinity;

		foreach (ref readonly Float3 point in points)
		{
			min = min.Min(point);
			max = max.Max(point);
		}

		Assert.IsTrue(max >= min);
	}

	public AxisAlignedBoundingBox(ReadOnlySpan<AxisAlignedBoundingBox> aabbs)
	{
		Assert.AreNotEqual(aabbs.Length, 0);

		min = Float3.PositiveInfinity;
		max = Float3.NegativeInfinity;

		foreach (ref readonly AxisAlignedBoundingBox aabb in aabbs)
		{
			min = min.Min(aabb.min);
			max = max.Max(aabb.max);
		}

		Assert.IsTrue(max >= min);
	}

	public AxisAlignedBoundingBox(ReadOnlySpan<AxisAlignedBoundingBox> aabbs, in Float4x4 transform)
	{
		Assert.AreNotEqual(aabbs.Length, 0);
		Float4x4 absolute = transform.Absoluted;

		min = Float3.PositiveInfinity;
		max = Float3.NegativeInfinity;

		foreach (ref readonly AxisAlignedBoundingBox aabb in aabbs)
		{
			Float3 center = transform.MultiplyPoint(aabb.Center);
			Float3 extend = absolute.MultiplyDirection(aabb.Extend);

			min = min.Min(center - extend);
			max = max.Max(center + extend);
		}

		Assert.IsTrue(max >= min);
	}

	public static readonly AxisAlignedBoundingBox none = new(Float3.PositiveInfinity, Float3.PositiveInfinity);

	public readonly Float3 min;
	public readonly Float3 max;

	public Float3 Center => (max + min) / 2f;
	public Float3 Extend => (max - min) / 2f;

	/// <summary>
	/// The full surface area of this <see cref="AxisAlignedBoundingBox"/>.
	/// </summary>
	public float Area => HalfArea * 2f;

	/// <summary>
	/// Half of <see cref="Area"/>.
	/// </summary>
	public float HalfArea
	{
		get
		{
			Float3 size = max - min;
			return size.X * (size.Y + size.Z) + size.Y * size.Z;
		}
	}

	/// <summary>
	/// Returns the axis (0 = x, 1 = y, 2 = z) that this <see cref="AxisAlignedBoundingBox"/> is the longest in.
	/// </summary>
	public int MajorAxis => (max - min).MaxIndex;

	/// <summary>
	/// Multiplier used on the far distance to remove floating point arithmetic errors when calculating
	/// an intersection with <see cref="AxisAlignedBoundingBox"/> by converting false-misses into false-hits.
	/// See https://www.arnoldrenderer.com/research/jcgt2013_robust_BVH-revised.pdf for details.
	/// </summary>
	public const float FarMultiplier = 1.00000024f;

	/// <summary>
	/// Tests intersection with bounding box. Returns distance to the nearest intersection point.
	/// NOTE: return can be negative, which means the ray origins inside this box.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Intersect(in Ray ray)
	{
		//The well known 'slab method'. Referenced from https://tavianator.com/2011/ray_box.html
		Float4 origin = (Float4)ray.origin;
		Float4 directionR = (Float4)ray.directionR;

		Float4 lengths0 = ((Float4)min - origin) * directionR;
		Float4 lengths1 = ((Float4)max - origin) * directionR;

		//Compute the min of the max lengths for far and the max of the min lengths for near
		Float4 lengthsMin = lengths0.Max(lengths1);
		Float4 lengthsMax = lengths0.Min(lengths1);

		//Get horizontal min and max
		float far = lengthsMin.Min(lengthsMin.YYYY.Min(lengthsMin.ZZZZ)).X;
		float near = lengthsMax.Max(lengthsMax.YYYY.Max(lengthsMax.ZZZZ)).X;

		far *= FarMultiplier;

		//NOTE: we place the infinity constant as the second return candidate because
		//if either near or far is NaN, this method will still return a valid result
		return (far >= near) & (far >= 0f) ? near : float.PositiveInfinity;
	}

	/// <summary>
	/// Returns a new <see cref="AxisAlignedBoundingBox"/> that encapsulates both
	/// this <see cref="AxisAlignedBoundingBox"/> and <paramref name="other"/>.
	/// </summary>
	public AxisAlignedBoundingBox Encapsulate(in AxisAlignedBoundingBox other) => new
	(
		min.Min(other.min),
		max.Max(other.max)
	);

	/// <summary>
	/// Fills <paramref name="span"/> with the eight vertices of this <see cref="AxisAlignedBoundingBox"/>.
	/// Note that <paramref name="span"/> must have a <see cref="Span{T}.Length"/> greater than or equals to
	/// eight. Returns the number elements used in <paramref name="span"/>, which is always eight.
	/// </summary>
	public int FillVertices(Span<Float3> span)
	{
		if (span.Length < 8) throw ExceptionHelper.Invalid(nameof(span.Length), span.Length, "is not large enough");

		span[0] = min;
		span[1] = max;

		span[2] = new Float3(min.X, min.Y, max.Z);
		span[3] = new Float3(min.X, max.Y, min.Z);
		span[4] = new Float3(max.X, min.Y, min.Z);

		span[5] = new Float3(max.X, max.Y, min.Z);
		span[6] = new Float3(max.X, min.Y, max.Z);
		span[7] = new Float3(min.X, max.Y, max.Z);

		return 8;
	}

	public override int GetHashCode() => unchecked((min.GetHashCode() * 397) ^ max.GetHashCode());

	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"{Center.ToString(format, provider)} ± {Extend.ToString(format, provider)}";
}