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
		public AxisAlignedBoundingBox(Float3 center, Float3 extend)
		{
			centerVector = default;
			extendVector = default;

			this.center = center;
			this.extend = extend;

			Assert.IsTrue(extend.MinComponent >= 0f);
		}

		public AxisAlignedBoundingBox(IReadOnlyList<AxisAlignedBoundingBox> aabb)
		{
			centerVector = default;
			extendVector = default;

			Float3 min = Float3.positiveInfinity;
			Float3 max = Float3.negativeInfinity;

			for (int i = 0; i < aabb.Count; i++)
			{
				AxisAlignedBoundingBox box = aabb[i];

				min = box.Min.Min(min);
				max = box.Max.Max(max);
			}

			extend = (max - min) / 2f;
			center = min + extend;

			Assert.IsTrue(extend.MinComponent >= 0f);
		}

		public AxisAlignedBoundingBox(ReadOnlySpan<Float3> points)
		{
			centerVector = default;
			extendVector = default;

			Float3 min = Float3.positiveInfinity;
			Float3 max = Float3.negativeInfinity;

			for (int i = 0; i < points.Length; i++)
			{
				Float3 point = points[i];

				min = min.Min(point);
				max = max.Max(point);
			}

			extend = (max - min) / 2f;
			center = min + extend;

			Assert.IsTrue(extend.MinComponent >= 0f);
		}

		[FieldOffset(0)] public readonly Float3 center;  //The exact center of the box
		[FieldOffset(12)] public readonly Float3 extend; //Half the size of the box

		[FieldOffset(0)] readonly Vector128<float> centerVector;
		[FieldOffset(12)] readonly Vector128<float> extendVector;

		public Float3 Min => center - extend;
		public Float3 Max => center + extend;

		public float Area => extend.x * extend.y + extend.x * extend.z + extend.y * extend.z;

		/// <summary>
		/// Tests intersection with bounding box. Returns distance to the nearest intersection point.
		/// NOTE: return can be negative, which means the ray origins inside this box.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe float Intersect(in Ray ray)
		{
			Vector128<float> n = Sse.Multiply(ray.inverseDirectionVector, Sse.Subtract(centerVector, ray.originVector));
			Vector128<float> k = Sse.Multiply(ray.absolutedInverseDirectionVector, extendVector);

			Vector128<float> min = Sse.Add(n, k);
			Vector128<float> max = Sse.Subtract(n, k);

			//Permute vector for min max, ignores last component
			Vector128<float> minPermute = Avx.Permute(min, 0b0100_1010);
			Vector128<float> maxPermute = Avx.Permute(max, 0b0100_1010);

			min = Sse.Min(min, minPermute);
			max = Sse.Max(max, maxPermute);

			//Second permute for min max
			minPermute = Avx.Permute(min, 0b1011_0001);
			maxPermute = Avx.Permute(max, 0b1011_0001);

			min = Sse.Min(min, minPermute);
			max = Sse.Max(max, maxPermute);

			//Extract result
			float far = *(float*)&min;
			float near = *(float*)&max;

			return near > far || far < 0f ? float.PositiveInfinity : near;
		}

		public AxisAlignedBoundingBox Encapsulate(AxisAlignedBoundingBox other)
		{
			Float3 min = Min.Min(other.Min);
			Float3 max = Max.Max(other.Max);

			Float3 extends = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extends, extends);
		}
	}
}