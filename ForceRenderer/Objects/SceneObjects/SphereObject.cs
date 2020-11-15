using System;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class SphereObject : SceneObject
	{
		public SphereObject(Material material, float radius) : base(material) => Radius = radius;

		public float Radius { get; set; }
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

		public float GetIntersection(in Ray ray)
		{
			float x = ray.origin.x - position.x;
			float y = ray.origin.y - position.y;
			float z = ray.origin.z - position.z;

			float point1 = -ray.direction.x * x - ray.direction.y * y - ray.direction.z * z;
			float point2Squared = point1 * point1 - x * x - y * y - z * z + radiusSquared;

			if (point2Squared < 0f) return float.PositiveInfinity;
			float point2 = (float)Math.Sqrt(point2Squared);

			if (point1 - point2 > 0f) return point1 - point2;
			if (point1 + point2 > 0f) return point1 + point2;

			return float.PositiveInfinity;
		}

		public Float3 GetNormal(Float3 point) => point.Normalized;
	}
}