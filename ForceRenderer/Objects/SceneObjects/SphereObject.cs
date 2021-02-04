using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Objects.SceneObjects
{
	public class SphereObject : SceneObject
	{
		public SphereObject(Material material, float radius) : base(material) => Radius = radius;

		public float Radius { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(Func<Material, int> materialConverter) => Enumerable.Empty<PressedTriangle>();

		public override IEnumerable<PressedSphere> ExtractSpheres(Func<Material, int> materialConverter)
		{
			int materialToken = materialConverter(Material);
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

		public AxisAlignedBoundingBox AABB => new AxisAlignedBoundingBox(position, (Float3)radius);

		/// <summary>
		/// Returns the distance of intersection between this sphere and <paramref name="ray"/> without backface culling.
		/// <paramref name="uv"/> contains the barycentric position of the intersection; if intersection does not exist, infinity is returned.
		/// </summary>
		public float GetIntersection(in Ray ray, out Float2 uv)
		{
			Float3 offset = ray.origin - position;

			float point1 = -offset.Dot(ray.direction);
			float point2Squared = point1 * point1 - offset.SquaredMagnitude + radiusSquared;

			if (point2Squared < 0f) goto noIntersection;

			float point2 = MathF.Sqrt(point2Squared);
			float result = point1 - point2;

			if (result < 0f) result = point1 + point2;
			if (result < 0f) goto noIntersection;

			Float3 point = offset + ray.direction * result;

			uv = new Float2
			(
				0.5f + MathF.Atan2(point.x, point.z) / Scalars.TAU,
				0.5f + MathF.Asin((point.y / radius).Clamp(-1f, 1f)) / Scalars.PI
			);

			return result;

			noIntersection:
			uv = default;
			return float.PositiveInfinity;
		}

		public Float3 GetNormal(Float2 uv)
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