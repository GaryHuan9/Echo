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

		public PreparedSphere(Float3 position, float radius, int materialToken)
		{
			this.position = position;
			this.radius = radius;

			radiusSquared = radius * radius;
			this.materialToken = materialToken;
		}

		public readonly Float3 position;
		public readonly float radius;

		public readonly float radiusSquared;
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
		/// barycentric coordinate of the intersection, otherwise, <see cref="float.PositiveInfinity"/> is returned.
		/// If intersection does not exist, <see cref="float.PositiveInfinity"/> is returned.
		/// NOTE: if <paramref name="findFar"/> is true, any intersection distance under <see cref="DistanceThreshold"/> is ignored.
		/// </summary>
		public float Intersect(in Ray ray, out Float2 uv, bool findFar = false)
		{
			const float Infinity = float.PositiveInfinity;
			Unsafe.SkipInit(out uv);

			//Test ray direction
			Float3 offset = ray.origin - position;
			float point0 = -offset.Dot(ray.direction);

			float point1Squared = point0 * point0 - offset.SquaredMagnitude + radiusSquared;

			if (point1Squared < 0f) return Infinity;

			//Find appropriate distance
			float point1 = FastMath.Sqrt0(point1Squared);
			float distance = point0 - point1;

			float threshold = findFar ? DistanceThreshold : 0f;

			if (distance < threshold) distance = point0 + point1;
			if (distance < threshold) return Infinity;

			//Calculate uv
			Float3 point = offset + ray.direction * distance;

			uv = new Float2
			(
				0.5f + MathF.Atan2(point.x, point.z) / Scalars.TAU,
				0.5f + MathF.Asin(FastMath.Clamp11(point.y / radius)) / Scalars.PI
			);

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
			float point0 = -offset.Dot(ray.direction);

			float point1Squared = point0 * point0 - offset.SquaredMagnitude + radiusSquared;

			if (point1Squared < 0f) return false;

			//Find appropriate distance
			float point1 = FastMath.Sqrt0(point1Squared);
			float distance = point0 - point1;

			float threshold = findFar ? DistanceThreshold : 0f;

			if (distance < threshold) distance = point0 + point1;
			return distance >= threshold && distance < travel;
		}

		public Float3 Sample(Distro2 distro) => distro.UniformSphere * radius;

		// public Float3 Sample(Distro2 distro, Float3 interaction)
		// {
		// 	Float3 difference = interaction - position;
		// 	float distance2 = difference.SquaredMagnitude;
		//
		// 	if (distance2 <= radiusSquared) return Sample(distro);
		//
		// 	//Find cosine max
		// 	float sinMaxT2 = radiusSquared / distance2;
		// 	float cosMaxT = FastMath.Sqrt0(1f - sinMaxT2);
		//
		// 	//Uniform sample cone, defined by theta and phi
		// 	float cosT = FastMath.FMA(distro.x, cosMaxT - 1f, 1f);
		// 	float sinT = FastMath.Identity(cosT);
		// 	float phi = distro.y * Scalars.TAU;
		//
		// 	//Compute angle alpha from center of sphere to sample point
		// 	float distance = FastMath.Sqrt0(distance2);
		// 	float project = distance * cosT - FastMath.Sqrt0(radiusSquared - distance2 * sinT * sinT);
		// 	float cosA = (distance2 + radiusSquared - project * project) / (2f * distance * radius);
		// 	float sinA = FastMath.Identity(cosA);
		//
		// 	//Find point
		// 	// Float3 point =radius *
		// 	throw new NotImplementedException();
		// }

		public static Float3 GetGeometryNormal(Float2 uv)
		{
			float angle0 = Scalars.TAU * (uv.x - 0.5f);
			float angle1 = Scalars.PI * (uv.y - 0.5f);

			FastMath.SinCos(angle0, out float sinT, out float cosT); //Theta
			FastMath.SinCos(angle1, out float sinP, out float cosP); //Phi

			return new Float3
			(
				sinT * cosP,
				sinP,
				cosT * cosP
			).Normalized;
		}
	}
}