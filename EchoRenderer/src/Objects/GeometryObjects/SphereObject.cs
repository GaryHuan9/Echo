using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.GeometryObjects
{
	public class SphereObject : GeometryObject
	{
		public SphereObject(Material material, float radius) : base(material) => Radius = radius;

		public float Radius { get; set; }

		public override IEnumerable<PreparedTriangle> ExtractTriangles(MaterialPreparer preparer) => Enumerable.Empty<PreparedTriangle>();

		public override IEnumerable<PreparedSphere> ExtractSpheres(MaterialPreparer preparer)
		{
			if (Radius <= 0f || FastMath.AlmostZero(Radius)) yield break;

			int materialToken = preparer.GetToken(Material);
			yield return new PreparedSphere(this, materialToken);
		}
	}

	public readonly struct PreparedSphere
	{
		public PreparedSphere(SphereObject sphere, int materialToken) : this
		(
			sphere.LocalToWorld.MultiplyPoint(Float3.zero),
			sphere.Scale.MaxComponent * sphere.Radius,
			materialToken
		) { }

		public PreparedSphere(in Float3 position, float radius, int materialToken)
		{
			this.position = position;
			this.radius = radius;
			this.materialToken = materialToken;
		}

		public readonly Float3 position;
		public readonly float radius;
		public readonly int materialToken;

		public AxisAlignedBoundingBox AABB => new(position - (Float3)radius, position + (Float3)radius);

		/// <summary>
		/// Because spheres have two intersection points, if an intersection distance is going to be under this value,
		/// we have the option to attempt to find the intersection that is further away. This is used to avoid self
		/// intersections along with <see cref="TraceQuery.ignore"/> and <see cref="OccludeQuery.ignore"/>.
		/// </summary>
		const float DistanceThreshold = 6e-4f;

		/// <summary>
		/// Returns the distance of intersection between this <see cref="PreparedSphere"/> and <paramref name="ray"/> without
		/// backface culling. If the intersection exists, the distance is returned and <paramref name="uv"/> will contain the
		/// a coordinate representing the surface location of the intersection, otherwise, <see cref="float.PositiveInfinity"/> is returned.
		/// NOTE: if <paramref name="findFar"/> is true, any intersection distance under <see cref="DistanceThreshold"/> is ignored.
		/// </summary>
		public float Intersect(in Ray ray, out Float2 uv, bool findFar = false)
		{
			const float Infinity = float.PositiveInfinity;
			Unsafe.SkipInit(out uv);

			//Test ray direction
			Float3 offset = ray.origin - position;
			float radiusSquared = radius * radius;
			float center = -offset.Dot(ray.direction);

			float extend2 = FastMath.FSA(center, radiusSquared - offset.SquaredMagnitude);

			if (extend2 < 0f) return Infinity;

			//Find appropriate distance
			float extend = FastMath.Sqrt0(extend2);
			float distance = center - extend;

			float threshold = findFar ? DistanceThreshold : 0f;

			if (distance < threshold) distance = center + extend;
			if (distance < threshold) return Infinity;

			//Calculate uv
			Float3 point = offset + ray.direction * distance;
			float sinP = FastMath.Clamp11(point.y / radius);
			float sinT = 0f;

			float smallRadius = FastMath.FMA(-point.y, point.y, radiusSquared);
			if (smallRadius > 0f) sinT = point.x * FastMath.SqrtR0(smallRadius);
			if (point.z < 0f) sinT += 3f; //Move sinT out of domain when cosT should be negative

			//Return
			uv = new Float2(sinT, sinP);
			return distance;
		}

		/// <summary>
		/// Returns whether <paramref name="ray"/> will intersect with this <see cref="PreparedSphere"/> after <paramref name="travel"/>.
		/// NOTE: if <paramref name="findFar"/> is true, any intersection distance under <see cref="DistanceThreshold"/> is ignored.
		/// </summary>
		public bool Intersect(in Ray ray, float travel, bool findFar = false)
		{
			//Test ray direction
			Float3 offset = ray.origin - position;
			float center = -offset.Dot(ray.direction);

			float squared = FastMath.FSA(center, FastMath.FSA(radius, -offset.SquaredMagnitude));

			if (squared < 0f) return false;

			//Find appropriate distance
			float extend = FastMath.Sqrt0(squared);
			float distance = center - extend;

			float threshold = findFar ? DistanceThreshold : 0f;

			if (distance < threshold) distance = center + extend;
			return distance >= threshold && distance < travel;
		}

		public GeometryPoint Sample(Distro2 distro) => Sample(distro.UniformSphere);

		public GeometryPoint Sample(Distro2 distro, in Float3 point)
		{
			Float3 offset = point - position;
			float radiusSquared = radius * radius;
			float distance2 = offset.SquaredMagnitude;

			if (distance2 <= radiusSquared) return Sample(distro);

			//Find cosine max
			float sinMaxT2 = radiusSquared / distance2;
			float cosMaxT = FastMath.Sqrt0(1f - sinMaxT2);

			//Uniform sample cone, defined by theta and phi
			float cosT = FastMath.FMA(distro.x, cosMaxT - 1f, 1f);
			float sinT = FastMath.Identity(cosT);
			float phi = distro.y * Scalars.TAU;

			//Compute angle alpha from center of sphere to sample point
			float distance = FastMath.Sqrt0(distance2);
			float project = distance * cosT - FastMath.Sqrt0(radiusSquared - distance2 * sinT * sinT);
			float cosA = (distance2 + radiusSquared - project * project) / (2f * distance * radius);
			float sinA = FastMath.Identity(cosA);

			//Find normal
			FastMath.SinCos(phi, out float sinP, out float cosP);
			Float3 normal = new Float3(sinA * cosP, sinA * sinP, cosA);

			var transform = new NormalTransform(offset);
			return Sample(transform.LocalToWorld(normal));
		}

		GeometryPoint Sample(in Float3 normal) => new(normal * radius + position, normal);

		public static Float3 GetNormal(Float2 uv)
		{
			ToThetaPhi(uv, out float sinT, out float sinP, out float cosT);
			float cosP = FastMath.Identity(sinP);
			return new Float3(sinT * cosP, sinP, cosT * cosP).Normalized;
		}

		public static Float2 GetTexcoord(Float2 uv)
		{
			ToThetaPhi(uv, out float sinT, out float sinP, out float cosT);

			return new Float2
			(
				FastMath.FMA(MathF.Atan2(sinT, cosT), 1f / Scalars.TAU, 0.5f),
				FastMath.FMA(MathF.Asin(FastMath.Clamp11(sinP)), 1f / Scalars.PI, 0.5f)
			);
		}

		/// <summary>
		/// Calculates and outputs the sines and cosines of theta and phi based on <paramref name="uv"/>.
		/// NOTE: cosine phi can be easily calculated by using <see cref="FastMath.Identity"/>.
		/// </summary>
		static void ToThetaPhi(Float2 uv, out float sinT, out float sinP, out float cosT)
		{
			sinT = uv.x;
			sinP = uv.y;

			float sign = 1f;

			if (sinT > 1.5f) //If sinT is out of domain, it means that cosT should be negative
			{
				sinT -= 3f;
				sign = -1f;
			}

			cosT = FastMath.Identity(sinT) * sign;
		}
	}
}