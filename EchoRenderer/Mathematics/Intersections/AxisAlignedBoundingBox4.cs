using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace EchoRenderer.Mathematics.Intersections
{
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

		readonly Vector128<float> minX;
		readonly Vector128<float> minY;
		readonly Vector128<float> minZ;

		readonly Vector128<float> maxX;
		readonly Vector128<float> maxY;
		readonly Vector128<float> maxZ;

		/// <summary>
		/// Finds the intersection between <paramref name="ray"/> and this <see cref="AxisAlignedBoundingBox4"/>.
		/// Returns either the intersection distance or <see cref="float.NegativeInfinity"/> if none was found.
		/// </summary>
		public Vector128<float> Intersect(in TraceRay ray)
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

			return Sse41.BlendVariable
			(
				near, Utilities.vectorNegativeInfinity, Sse.Or
				(
					Sse.CompareGreaterThan(near, far),
					Sse.CompareGreaterThan(Utilities.vector0, far)
				)
			);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Utilities.GetHashCode(minX);
				hashCode = (hashCode * 397) ^ Utilities.GetHashCode(minY);
				hashCode = (hashCode * 397) ^ Utilities.GetHashCode(minZ);
				hashCode = (hashCode * 397) ^ Utilities.GetHashCode(maxX);
				hashCode = (hashCode * 397) ^ Utilities.GetHashCode(maxY);
				hashCode = (hashCode * 397) ^ Utilities.GetHashCode(maxZ);
				return hashCode;
			}
		}

		static Vector128<float> Make(float value) => Vector128.Create(value);
		static Vector128<float> Make(float value0, float value1, float value2, float value3) => Vector128.Create(value0, value1, value2, value3);
	}
}