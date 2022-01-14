using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Preparation;
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

		const float DistanceMin = PreparedPack.DistanceMin;
		const float Infinity = float.PositiveInfinity;

		/// <summary>
		/// Returns the distance of intersection between this <see cref="PreparedSphere"/> and <paramref name="ray"/> without
		/// backface culling. If the intersection exists, the distance is returned and <paramref name="uv"/> will contain the
		/// barycentric coordinate of the intersection, otherwise, <see cref="float.PositiveInfinity"/> is returned.
		/// If intersection does not exist, <see cref="float.PositiveInfinity"/> is returned.
		/// </summary>
		public float Intersect(in Ray ray, out Float2 uv)
		{
			Unsafe.SkipInit(out uv);

			Float3 offset = ray.origin - position;
			float point0 = -offset.Dot(ray.direction);

			float point1Squared = point0 * point0 - offset.SquaredMagnitude + radiusSquared;

			if (point1Squared < 0f) return Infinity;

			float point1 = FastMath.Sqrt0(point1Squared);
			float distance = point0 - point1;

			if (distance < DistanceMin) distance = point0 + point1;
			if (distance < DistanceMin) return Infinity;

			Float3 point = offset + ray.direction * distance;

			uv = new Float2
			(
				0.5f + MathF.Atan2(point.x, point.z) / Scalars.TAU,
				0.5f + MathF.Asin((point.y / radius).Clamp(-1f)) / Scalars.PI
			);

			return distance;
		}

		/// <summary>
		/// Returns whether <paramref name="ray"/> will intersect with this <see cref="PreparedSphere"/> after <paramref name="travel"/>.
		/// </summary>
		public bool Intersect(in Ray ray, float travel)
		{
			Float3 offset = ray.origin - position;
			float point0 = -offset.Dot(ray.direction);

			float point1Squared = point0 * point0 - offset.SquaredMagnitude + radiusSquared;

			if (point1Squared < 0f) return false;

			float point1 = FastMath.Sqrt0(point1Squared);
			float distance = point0 - point1;

			if (distance < DistanceMin) distance = point0 + point1;
			return distance >= DistanceMin && distance <= travel;
		}

		public static Float3 GetGeometryNormal(Float2 uv)
		{
			//TODO: account for rotation during uv calculation, and in turn normal calculation

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