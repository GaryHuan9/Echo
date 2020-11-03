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
		float radiusSquared;

		public override float GetRawIntersection(in Ray ray)
		{
			float point1 = -ray.direction.Dot(ray.origin);
			float point2Squared = point1 * point1 - ray.origin.SquaredMagnitude + radiusSquared;

			if (point2Squared < 0f) return float.PositiveInfinity;
			float point2 = (float)Math.Sqrt(point2Squared);

			if (point1 - point2 > 0f) return point1 - point2;
			if (point1 + point2 > 0f) return point1 + point2;

			return float.PositiveInfinity;
		}

		public override Float3 GetRawNormal(Float3 point) => point.Normalized;

		public override void OnPressed()
		{
			base.OnPressed();
			radiusSquared = Radius * Radius;
		}
	}
}