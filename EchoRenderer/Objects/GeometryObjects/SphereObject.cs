using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.GeometryObjects
{
	public class SphereObject : GeometryObject
	{
		public SphereObject(Material material, float radius) : base(material) => Radius = radius;

		public float Radius { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(MaterialPresser presser) => Enumerable.Empty<PressedTriangle>();

		public override IEnumerable<PressedSphere> ExtractSpheres(MaterialPresser presser)
		{
			int materialToken = presser.GetToken(Material);
			yield return new PressedSphere(this, materialToken);
		}
	}

	public readonly struct PressedSphere
	{
		public PressedSphere(SphereObject sphere, int materialToken) : this
		(
			sphere.LocalToWorld.MultiplyPoint(Float3.zero),
			sphere.Scale.MaxComponent * sphere.Radius,
			materialToken
		) { }

		public PressedSphere(Float3 position, float radius, int materialToken)
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

		public AxisAlignedBoundingBox AABB => new AxisAlignedBoundingBox(position - (Float3)radius, position + (Float3)radius);

		const float DistanceMin = PressedPack.DistanceMin;

		/// <summary>
		/// Returns the distance of intersection between this sphere and <paramref name="ray"/> without backface culling.
		/// <paramref name="uv"/> contains the barycentric position of the intersection.
		/// If intersection does not exist, <see cref="float.PositiveInfinity"/> is returned.
		/// </summary>
		public float GetIntersection(in Ray ray, out Float2 uv)
		{
			Float3 offset = ray.origin - position;

			float point1 = -offset.Dot(ray.direction);
			float point2Squared = point1 * point1 - offset.SquaredMagnitude + radiusSquared;

			if (point2Squared < 0f) goto noIntersection;

			float point2 = MathF.Sqrt(point2Squared);
			float result = point1 - point2;

			if (result < DistanceMin) result = point1 + point2;
			if (result < DistanceMin) goto noIntersection;

			Float3 point = offset + ray.direction * result;

			uv = new Float2
			(
				0.5f + MathF.Atan2(point.x, point.z) / Scalars.TAU,
				0.5f + MathF.Asin((point.y / radius).Clamp(-1f)) / Scalars.PI
			);

			return result;

			noIntersection:
			Unsafe.SkipInit(out uv);
			return float.PositiveInfinity;
		}

		/// <summary>
		/// Returns the distance of intersection between this sphere and <paramref name="ray"/> without backface culling.
		/// If intersection does not exist, <see cref="float.PositiveInfinity"/> is returned.
		/// </summary>
		public float GetIntersection(in Ray ray)
		{
			Float3 offset = ray.origin - position;

			float point1 = -offset.Dot(ray.direction);
			float point2Squared = point1 * point1 - offset.SquaredMagnitude + radiusSquared;

			if (point2Squared < 0f) return float.PositiveInfinity;

			float point2 = MathF.Sqrt(point2Squared);
			float result = point1 - point2;

			if (result < DistanceMin) result = point1 + point2;
			if (result < DistanceMin) return float.PositiveInfinity;

			return result;
		}

		public static Float3 GetNormal(Float2 uv)
		{
			//TODO: account for rotation during uv calculation, and in turn normal calculation

			float theta = Scalars.TAU * (uv.x - 0.5f);
			float phi = Scalars.PI * (uv.y - 0.5f);
			float cos = MathF.Cos(phi);

			return new Float3
			(
				MathF.Sin(theta) * cos,
				MathF.Sin(phi),
				MathF.Cos(theta) * cos
			).Normalized;
		}
	}
}