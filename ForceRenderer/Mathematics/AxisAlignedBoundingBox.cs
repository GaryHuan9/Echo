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

		/// <summary>
		/// Tests intersection with bounding box. Returning vector x is the near distance (NOTE: might be negative)
		/// vector y is far distance (will not be negative). Negative near means ray origins inside box.
		/// </summary>
		public Float2 Intersect(in Ray ray)
		{
			Float3 n = ray.inverseDirection * (ray.origin - center);
			Float3 k = ray.inverseDirection.Absoluted * extend;

			float near = (n - k).MaxComponent;
			float far = (n + k).MinComponent;

			return near > far || far < 0f ? Float2.positiveInfinity : new Float2(near, far);
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

			for (int i = 0; i < length; i++)
			{
				AxisAlignedBoundingBox box = boxes[i + start];

				max = box.Max.Max(max);
				min = box.Min.Min(min);
			}

			Float3 extend = (max - min) / 2f;
			return new AxisAlignedBoundingBox(min + extend, extend);
		}

		/// <inheritdoc cref="Construct(System.Collections.Generic.IReadOnlyList{ForceRenderer.Mathematics.AxisAlignedBoundingBox})"/>
		public static AxisAlignedBoundingBox Construct(IReadOnlyList<AxisAlignedBoundingBox> boxes, IReadOnlyList<int> indices)
		{
			Float3 max = Float3.negativeInfinity;
			Float3 min = Float3.positiveInfinity;

			for (int i = 0; i < indices.Count; i++)
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