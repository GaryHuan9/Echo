using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Intersections
{
	[StructLayout(LayoutKind.Explicit, Size = 28)]
	public readonly struct AxisAlignedBoundingBox
	{
		public AxisAlignedBoundingBox(Float3 min, Float3 max)
		{
			Unsafe.SkipInit(out minVector);
			Unsafe.SkipInit(out maxVector);

			this.min = min;
			this.max = max;

			Assert.IsTrue(max >= min);
		}

		public AxisAlignedBoundingBox(IReadOnlyList<AxisAlignedBoundingBox> aabb)
		{
			Unsafe.SkipInit(out minVector);
			Unsafe.SkipInit(out maxVector);

			min = Float3.positiveInfinity;
			max = Float3.negativeInfinity;

			for (int i = 0; i < aabb.Count; i++)
			{
				AxisAlignedBoundingBox box = aabb[i];

				min = box.min.Min(min);
				max = box.max.Max(max);
			}

			Assert.IsTrue(max >= min);
		}

		public AxisAlignedBoundingBox(ReadOnlySpan<Float3> points)
		{
			Unsafe.SkipInit(out minVector);
			Unsafe.SkipInit(out maxVector);

			min = Float3.positiveInfinity;
			max = Float3.negativeInfinity;

			for (int i = 0; i < points.Length; i++)
			{
				Float3 point = points[i];

				min = min.Min(point);
				max = max.Max(point);
			}

			Assert.IsTrue(max >= min);
		}

		public static readonly AxisAlignedBoundingBox none = new(Float3.negativeInfinity, Float3.negativeInfinity);

		[FieldOffset(0)]  public readonly Float3 min;
		[FieldOffset(12)] public readonly Float3 max;

		[FieldOffset(0)]  readonly Vector128<float> minVector;
		[FieldOffset(12)] readonly Vector128<float> maxVector;

		public Float3 Center => (max + min) / 2f;
		public Float3 Extend => (max - min) / 2f;

		public float Area
		{
			get
			{
				Float3 size = max - min;
				return size.x * size.y + size.x * size.z + size.y * size.z;
			}
		}

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
		public unsafe float Intersect(in Ray ray)
		{
			//The well known 'slab method'. Referenced from https://tavianator.com/2011/ray_box.html

			Vector128<float> lengths0 = Sse.Multiply(Sse.Subtract(minVector, ray.originVector), ray.inverseDirectionVector);
			Vector128<float> lengths1 = Sse.Multiply(Sse.Subtract(maxVector, ray.originVector), ray.inverseDirectionVector);

			Vector128<float> lengthsMin = Sse.Max(lengths0, lengths1);
			Vector128<float> lengthsMax = Sse.Min(lengths0, lengths1);

			//The previous two lines could be a bit confusing: we are trying to find
			//the min of the max lengths for far and the max of the min lengths for near

			float far;
			float near;

			//Compute horizontal min and max

			if (Avx.IsSupported)
			{
				//Permute vector for min max, ignore last component
				Vector128<float> minPermute = Avx.Permute(lengthsMin, 0b0100_1010);
				Vector128<float> maxPermute = Avx.Permute(lengthsMax, 0b0100_1010);

				lengthsMin = Sse.Min(lengthsMin, minPermute);
				lengthsMax = Sse.Max(lengthsMax, maxPermute);

				//Second permute for min max
				minPermute = Avx.Permute(lengthsMin, 0b1011_0001);
				maxPermute = Avx.Permute(lengthsMax, 0b1011_0001);

				lengthsMin = Sse.Min(lengthsMin, minPermute);
				lengthsMax = Sse.Max(lengthsMax, maxPermute);

				//Extract result
				far = *(float*)&lengthsMin;
				near = *(float*)&lengthsMax;
			}
			else
			{
				//Software implementation
				far = (*(Float3*)&lengthsMin).MinComponent;
				near = (*(Float3*)&lengthsMax).MaxComponent;
			}

			far *= FarMultiplier;

			//NOTE: we place the infinity constant as the second return candidate because
			//if either near or far is NaN, this method will still return a valid result
			return far >= near && far >= 0f ? near : float.PositiveInfinity;
		}

		public AxisAlignedBoundingBox Encapsulate(AxisAlignedBoundingBox other) => new(min.Min(other.min), max.Max(other.max));

		public override int GetHashCode() => unchecked((min.GetHashCode() * 397) ^ max.GetHashCode());
	}
}