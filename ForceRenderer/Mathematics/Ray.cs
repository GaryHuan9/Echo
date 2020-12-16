using System.Diagnostics;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics
{
	public readonly struct Ray
	{
		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		/// <param name="forwardShift">Whether you want to create the ray so it is shifted a bit forward to avoid intersection with itself.</param>
		public Ray(Float3 origin, Float3 direction, bool forwardShift) : this
		(
			forwardShift ? origin + direction * 5E-4f : origin,
			direction
		) { }

		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		public Ray(Float3 origin, Float3 direction)
		{
			Debug.Assert(Scalars.AlmostEquals(direction.SquaredMagnitude, 1f));

			this.origin = origin;
			this.direction = direction;

			inverseDirection = (1f / direction).Clamp(Float3.minValue, Float3.maxValue);
		}

		public readonly Float3 origin;
		public readonly Float3 direction;
		public readonly Float3 inverseDirection;

		public Float3 GetPoint(float distance) => origin + direction * distance;

		public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
	}
}