using System.Runtime.CompilerServices;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;

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
		public Ray(Float3 origin, Float3 direction, bool forwardShift)
		{
			this.origin = forwardShift ? origin + direction * 5E-4f : origin;
			this.direction = direction;
		}

		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		public Ray(Float3 origin, Float3 direction)
		{
			this.origin = origin;
			this.direction = direction;
		}

		public readonly Float3 origin;
		public readonly Float3 direction;

		public Float3 GetPoint(float distance) => origin + direction * distance;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public Ray TransformForward(in Transformation value) => new Ray(value.Forward(origin), value.ForwardDirection(direction));
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public Ray TransformBackward(in Transformation value) => new Ray(value.Backward(origin), value.BackwardDirection(direction));

		public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
	}
}