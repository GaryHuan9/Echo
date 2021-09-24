using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.Vector128;
using static EchoRenderer.Mathematics.Utilities;

namespace EchoRenderer.Mathematics.Intersections
{
	public readonly unsafe struct AxisAlignedBoundingBox4
	{
		readonly Vector128<float> minX;
		readonly Vector128<float> minY;
		readonly Vector128<float> minZ;

		readonly Vector128<float> maxX;
		readonly Vector128<float> maxY;
		readonly Vector128<float> maxZ;

		public Vector128<float> Intersect(in TracerRay ray)
		{
			Vector128<float> length0X = Sse.Multiply(Sse.Subtract(minX, Create(ray.origin.x)), Create(ray.inverseDirection.x));
			Vector128<float> length0Y = Sse.Multiply(Sse.Subtract(minY, Create(ray.origin.y)), Create(ray.inverseDirection.y));
			Vector128<float> length0Z = Sse.Multiply(Sse.Subtract(minZ, Create(ray.origin.z)), Create(ray.inverseDirection.z));

			Vector128<float> length1X = Sse.Multiply(Sse.Subtract(maxX, Create(ray.origin.x)), Create(ray.inverseDirection.x));
			Vector128<float> length1Y = Sse.Multiply(Sse.Subtract(maxY, Create(ray.origin.y)), Create(ray.inverseDirection.y));
			Vector128<float> length1Z = Sse.Multiply(Sse.Subtract(maxZ, Create(ray.origin.z)), Create(ray.inverseDirection.z));

			Vector128<float> lengthMinX = Sse.Max(length0X, length1X);
			Vector128<float> lengthMinY = Sse.Max(length0Y, length1Y);
			Vector128<float> lengthMinZ = Sse.Max(length0Z, length1Z);

			Vector128<float> lengthMaxX = Sse.Min(length0X, length1X);
			Vector128<float> lengthMaxY = Sse.Min(length0Y, length1Y);
			Vector128<float> lengthMaxZ = Sse.Min(length0Z, length1Z);

			Vector128<float> far = Sse.Min(lengthMinX, Sse.Min(lengthMinY, lengthMinZ));
			Vector128<float> near = Sse.Max(lengthMaxX, Sse.Max(lengthMaxY, lengthMaxZ));

			Vector128<float> condition = Sse.Or(Sse.CompareGreaterThan(near, far), Sse.CompareGreaterThan(vector0, far));
			return Sse41.BlendVariable(vectorPositiveInfinity, near, condition);
		}
	}
}