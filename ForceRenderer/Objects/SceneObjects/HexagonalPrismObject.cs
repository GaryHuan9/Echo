using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects.SceneObjects
{
	public class HexagonalPrismObject : SceneObject
	{
		public HexagonalPrismObject(float radius, float height)
		{
			Radius = radius;
			Height = height;
		}

		public float Radius { get; set; }
		public float Height { get; set; }

		public override float GetSignedDistanceRaw(Float3 point)
		{
			const float kX = -0.8660254f;
			const float kY = 0.5f;
			const float kZ = 0.57735f;

			point = point.Absoluted;

			float v = 2f * Math.Min(kX * point.x + kY * point.y, 0f);

			float x = point.x - v * kX;
			float y = point.y - v * kY;

			float dX = (y - Radius).Sign() * Magnitude(x - x.Clamp(-kZ * Radius, kZ * Radius), y - Radius);
			float dY = point.z - Height;

			return Math.Min(Math.Max(dX, dY), 0f) + Magnitude(Math.Max(dX, 0f), Math.Max(dY, 0f));
		}

		static float Magnitude(float x, float y) => (float)Math.Sqrt(x * x + y * y);
	}
}