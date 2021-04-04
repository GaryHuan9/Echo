using System;
using System.Threading;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics
{
	public class ExtendedRandom : Random
	{
		public ExtendedRandom() : base(Thread.CurrentThread.ManagedThreadId ^ Environment.TickCount) { }

		public float NextFloat() => (float)NextDouble();
		public float NextFloat(float max) => NextFloat() * max;
		public float NextFloat(float min, float max) => NextFloat(max - min) + min;

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
	}
}