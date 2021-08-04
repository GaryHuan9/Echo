using System;
using System.Threading;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics
{
	public class ExtendedRandom : Random
	{
		public ExtendedRandom(int seed) : base(seed) { }
		public ExtendedRandom() : this(HashCode.Combine(Thread.CurrentThread.ManagedThreadId, Environment.TickCount64)) { }

		public float NextFloat() => (float)NextDouble();
		public float NextFloat(float max) => NextFloat() * max;
		public float NextFloat(float min, float max) => NextFloat(max - min) + min;

		public Float2 NextFloat2() => new Float2(NextFloat(), NextFloat());
		public Float2 NextFloat2(float max) => new Float2(NextFloat(max), NextFloat(max));
		public Float2 NextFloat2(float min, float max) => new Float2(NextFloat(min, max), NextFloat(min, max));

		public Float3 NextFloat3() => new Float3(NextFloat(), NextFloat(), NextFloat());
		public Float3 NextFloat3(float max) => new Float3(NextFloat(max), NextFloat(max), NextFloat(max));
		public Float3 NextFloat3(float min, float max) => new Float3(NextFloat(min, max), NextFloat(min, max), NextFloat(min, max));

		/// <summary>
		/// Returns a random vector inside a unit sphere.
		/// </summary>
		public Float3 NextInSphere()
		{
			Float3 random;

			do random = NextFloat3(-1f, 1f);
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
			float u0 = 1f - NextFloat();
			float u1 = 1f - NextFloat();

			return MathF.Sqrt(-2f * MathF.Log(u0)) * MathF.Sin(2f * Scalars.PI * u1);
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