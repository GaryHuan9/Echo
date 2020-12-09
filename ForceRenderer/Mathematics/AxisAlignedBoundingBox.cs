using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Vectors;

namespace ForceRenderer.Mathematics
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct AxisAlignedBoundingBox
	{
		public AxisAlignedBoundingBox(Float3 center, Float3 extend)
		{
			this.center = center;
			this.extend = extend;
		}

		public readonly Float3 center; //The exact center of the box
		public readonly Float3 extend; //Half the size of the box

		public Float3 Max => center + extend;
		public Float3 Min => center - extend;

		public float Area => (extend.x * extend.y + extend.x * extend.z + extend.y * extend.z) * 8f;

		/// <summary>
		/// Tests intersection with bounding box. Returns distance to the nearest intersection point.
		/// NOTE: return can be negative, which means the ray origins inside box.
		/// </summary>
		public float Intersect(in Ray ray)
		{
			Float3 n = ray.inverseDirection * (center - ray.origin);
			Float3 k = ray.inverseDirection.Absoluted * extend;

			float near = (n - k).MaxComponent;
			float far = (n + k).MinComponent;

			return near > far || far < 0f ? float.PositiveInfinity : near;
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
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes) => Construct(boxes, 0, boxes.Count);

		/// <inheritdoc cref="Construct(System.Collections.Generic.IReadOnlyList{ForceRenderer.Mathematics.AxisAlignedBoundingBox})"/>
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes, int start, int length)
		{
			Float3 max = Float3.negativeInfinity;
			Float3 min = Float3.positiveInfinity;

			for (int i = start; i < start + length; i++)
			{
				AxisAlignedBoundingBox box = boxes[i];

				max = box.Max.Max(max);
				min = box.Min.Min(min);
			}

			Float3 extend = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extend, extend);
		}

		/// <inheritdoc cref="Construct(System.Collections.Generic.IReadOnlyList{ForceRenderer.Mathematics.AxisAlignedBoundingBox})"/>
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes, IReadOnlyList<int> indices) => Construct(boxes, indices, 0, indices.Count);

		/// <inheritdoc cref="Construct(System.Collections.Generic.IReadOnlyList{ForceRenderer.Mathematics.AxisAlignedBoundingBox})"/>
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes, IReadOnlyList<int> indices, int start, int length)
		{
			Float3 max = Float3.negativeInfinity;
			Float3 min = Float3.positiveInfinity;

			for (int i = start; i < start + length; i++)
			{
				AxisAlignedBoundingBox box = boxes[indices[i]];

				max = box.Max.Max(max);
				min = box.Min.Min(min);
			}

			Float3 extend = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extend, extend);
		}
	}
}