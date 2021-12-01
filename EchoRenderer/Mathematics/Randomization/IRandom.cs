using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Randomization
{
	public interface IRandom
	{
		/// <summary>
		/// Returns the next pseudorandom number between zero (inclusive) and one (exclusive).
		/// </summary>
		public float Value { get; }

		/// <summary>
		/// Returns the next <see cref="Value"/> between zero (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public float Next(float max) => Value * max;

		/// <summary>
		/// Returns the next <see cref="Value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public float Next(float min, float max)
		{
			float distance = max - min;
			return MathF.FusedMultiplyAdd(distance, Value, min);
		}

		/// <summary>
		/// Returns the next two <see cref="Value"/> between zero (inclusive) and one (exclusive).
		/// </summary>
		public Float2 Next2() => new(Value, Value);

		/// <summary>
		/// Returns the next two <see cref="Value"/> between zero (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public Float2 Next2(float max) => new(Next(max), Next(max));

		/// <summary>
		/// Returns the next two <see cref="Value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public Float2 Next2(float min, float max) => new(Next(min, max), Next(min, max));

		/// <summary>
		/// Returns the next three <see cref="Value"/> between zero (inclusive) and one (exclusive).
		/// </summary>
		public Float3 Next3() => new(Value, Value, Value);

		/// <summary>
		/// Returns the next three <see cref="Value"/> between zero (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public Float3 Next3(float max) => new(Next(max), Next(max), Next(max));

		/// <summary>
		/// Returns the next three <see cref="Value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public Float3 Next3(float min, float max) => new(Next(min, max), Next(min, max), Next(min, max));

		/// <summary>
		/// Returns a random vector inside a unit sphere.
		/// </summary>
		public Float3 NextInSphere()
		{
			Float3 random;

			do random = Next3(-1f, 1f);
			while (random.SquaredMagnitude > 1f);

			return random;
		}

		/// <summary>
		/// Returns a random vector inside a sphere with <paramref name="radius"/>.
		/// </summary>
		public Float3 NextInSphere(float radius) => NextInSphere() * radius;

		/// <summary>
		/// Returns a random unit vector that is on a unit sphere.
		/// </summary>
		public Float3 NextOnSphere() => NextInSphere().Normalized;

		/// <summary>
		/// Returns a random vector that is on a sphere with <paramref name="radius"/>.
		/// </summary>
		public Float3 NextOnSphere(float radius) => NextInSphere().Normalized * radius;

		/// <summary>
		/// Returns a random value on the gaussian distribution curve. Implementation based
		/// on the Box-Muller transform with a standard deviation of 1 and mean of 0.
		/// </summary>
		public float NextGaussian()
		{
			float u0 = 1f - Value;
			float u1 = 1f - Value;

			return FastMath.Sqrt0(-2f * MathF.Log(u0)) * MathF.Sin(Scalars.TAU * u1);
		}

		/// <summary>
		/// Returns a randomly gaussian distributed point with mean
		/// at (0.5, 0.5) and clamped between (0, 0) and (1, 1).
		/// </summary>
		public Float2 NextSample()
		{
			Float2 position = new Float2(NextGaussian(), NextGaussian()) / 6f;
			return position.Clamp(Float2.negativeHalf, Float2.half) + Float2.half;
		}
	}
}