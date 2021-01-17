using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics
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
		}

		[FieldOffset(0)] public readonly Float3 center;  //The exact center of the box
		[FieldOffset(12)] public readonly Float3 extend; //Half the size of the box

		[FieldOffset(0)] readonly Vector128<float> centerVector;
		[FieldOffset(12)] readonly Vector128<float> extendVector;

		public Float3 Max => center + extend;
		public Float3 Min => center - extend;

		public float Area => (extend.x * extend.y + extend.x * extend.z + extend.y * extend.z) * 8f;

		/// <summary>
		/// Tests intersection with bounding box. Returns distance to the nearest intersection point.
		/// NOTE: return can be negative, which means the ray origins inside box.
		/// </summary>
		public float Intersect(in Ray ray)
		{
			Vector128<float> n = Sse.Multiply(ray.inverseDirectionVector, Sse.Subtract(centerVector, ray.originVector));
			Vector128<float> k = Sse.Multiply(ray.absolutedInverseDirectionVector, extendVector);

			Vector128<float> min = Sse.Add(n, k);
			Vector128<float> max = Sse.Subtract(n, k);

			unsafe
			{
				float far = (*(Float3*)&min).MinComponent;
				float near = (*(Float3*)&max).MaxComponent;

				return near > far || far < 0f ? float.PositiveInfinity : near;
			}
		}

		public AxisAlignedBoundingBox Encapsulate(AxisAlignedBoundingBox other)
		{
			Float3 max = Max.Max(other.Max);
			Float3 min = Min.Min(other.Min);

			Float3 extends = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extends, extends);
		}

		/// <summary>
		/// Constructs a new <see cref="AxisAlignedBoundingBox"/> by bounding a selection of smaller bounding boxes.
		/// </summary>
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes)
		{
			Float3 max = Float3.negativeInfinity;
			Float3 min = Float3.positiveInfinity;

			for (int i = 0; i < boxes.Count; i++)
			{
				AxisAlignedBoundingBox box = boxes[i];

				max = box.Max.Max(max);
				min = box.Min.Min(min);
			}

			Float3 extend = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extend, extend);
		}
	}
}