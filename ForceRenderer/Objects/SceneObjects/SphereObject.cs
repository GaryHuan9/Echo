using System;
using System.Collections.Generic;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class SphereObject : SceneObject
	{
		public SphereObject(Material material, float radius) : base(material) => Radius = radius;

		public float Radius { get; set; }

		public override void Press(List<PressedTriangle> triangles, List<PressedSphere> spheres, int materialToken)
		{
			spheres.Add(new PressedSphere(this, materialToken));
		}
	}

	public readonly struct PressedSphere
	{
		public PressedSphere(SphereObject sphere, int materialToken)
		{
			position = sphere.LocalToWorld.MultiplyPoint(Float3.zero);
			radius = sphere.Scale.MaxComponent * sphere.Radius;

			radiusSquared = radius * radius;
			this.materialToken = materialToken;
		}

		public readonly Float3 position;
		public readonly float radius;

		public readonly float radiusSquared;
		public readonly int materialToken;

		public AxisAlignedBoundingBox AABB => new AxisAlignedBoundingBox(position, (Float3)radius);

		public float GetIntersection(in Ray ray)
		{
			Float3 offset = ray.origin - position;

			float point1 = -offset.Dot(ray.direction);
			float point2Squared = point1 * point1 - offset.SquaredMagnitude + radiusSquared;

			if (point2Squared < 0f) return float.PositiveInfinity;
			float point2 = (float)Math.Sqrt(point2Squared);

			if (point1 - point2 > 0f) return point1 - point2;
			if (point1 + point2 > 0f) return point1 + point2;

			return float.PositiveInfinity;
		}

		public Float3 GetNormal(Float3 point) => (point - position).Normalized;
	}
}