using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Rendering.Sampling;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// The base class to either bidirectional reflectance distribution function or bidirectional transmittance distribution function.
	/// </summary>
	public abstract class BxDF
	{
		protected BxDF(FunctionType type) => functionType = type;

		public readonly FunctionType functionType;

		public bool MatchType(FunctionType type) => (functionType & type) == type;
		public bool HasType(FunctionType   type) => (functionType & type) != 0;

		/// <summary>
		/// Samples and returns the value of the distribution function from two pairs of
		/// directions, <paramref name="incident"/> and <paramref name="outgoing"/>.
		/// </summary>
		public abstract Float3 Sample(in Float3 outgoing, in Float3 incident);

		/// <summary>
		/// Samples and returns the value of the distribution function from <paramref name="outgoing"/>, and outputs
		/// the scattering direction to <paramref name="incident"/>. Used by delta distributions (eg. perfect specular)
		/// </summary>
		public virtual Float3 Sample(in Float3 outgoing, out Float3 incident, in Sample2 sample, out float pdf)
		{
			incident = sample.CosineHemisphere;
			if (outgoing.z < 0f) incident = new Float3(incident.x, incident.y, -incident.z);

			pdf = ProbabilityDensity(outgoing, incident);
			return Sample(outgoing, incident);
		}

		/// <summary>
		/// Returns the hemispherical-directional reflectance, the total reflectance in direction
		/// <paramref name="outgoing"/> due to a constant illumination over the doming hemisphere
		/// </summary>
		public virtual Float3 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2> samples)
		{
			Float3 result = Float3.zero;

			foreach (Sample2 sample in samples)
			{
				Float3 sampled = Sample(outgoing, out Float3 incident, sample, out float pdf);
				if (pdf > 0f) result += sampled * AbsoluteCosine(incident) / pdf;
			}

			return result / samples.Length;
		}

		/// <summary>
		/// Returns the hemispherical-hemispherical reflectance, the fraction of incident light
		/// reflected when the amount of incident light is constant across all directions
		/// </summary>
		public virtual Float3 GetReflectance(ReadOnlySpan<Sample2> samples0, ReadOnlySpan<Sample2> samples1)
		{
			Float3 result = Float3.zero;
			int length = samples0.Length;

			Assert.AreEqual(length, samples1.Length);
			for (int i = 0; i < samples0.Length; i++)
			{
				Sample2 sample = samples0[i];

				Float3 outgoing = sample.UniformHemisphere;
				Float3 sampled = Sample(outgoing, out Float3 incident, samples1[i], out float pdf);

				pdf *= sample.UniformHemispherePdf;

				if (pdf > 0f) result += sampled * AbsoluteCosine(outgoing) * AbsoluteCosine(incident) / pdf;
			}

			return result / length;
		}

		/// <summary>
		/// Returns the sampled probability density (pdf) for a given pair of
		/// <paramref name="outgoing"/> and <paramref name="incident"/> directions.
		/// </summary>
		public virtual float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
		{
			if (!SameHemisphere(outgoing, incident)) return 0f;
			return AbsoluteCosine(incident) * (1f / Scalars.PI);
		}

		/// <summary>
		/// Returns the cosine value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Cosine(in Float3 direction) => direction.z;

		/// <summary>
		/// Returns the cosine squared value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Cosine2(in Float3 direction) => direction.z * direction.z;

		/// <summary>
		/// Returns the absolute cosine value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float AbsoluteCosine(in Float3 direction) => FastMath.Abs(direction.z);

		/// <summary>
		/// Returns the sine squared value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Sine2(in Float3 direction) => FastMath.Max0(1f - Cosine2(direction));

		/// <summary>
		/// Returns the sine value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Sine(in Float3 direction) => MathF.Sqrt(Sine2(direction));

		/// <summary>
		/// Returns the tangent value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Tangent(in Float3 direction) => Sine(direction) / Cosine(direction);

		/// <summary>
		/// Returns the tangent squared value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Tangent2(in Float3 direction) => Sine2(direction) / Cosine2(direction);

		/// <summary>
		/// Returns the cosine value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float CosinePhi(in Float3 direction)
		{
			float sin = Sine(direction);
			if (sin == 0f) return 1f;
			return FastMath.Clamp11(direction.x / sin);
		}

		/// <summary>
		/// Returns the sine value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float SinePhi(in Float3 direction)
		{
			float sin = Sine(direction);
			if (sin == 0f) return 0f;
			return FastMath.Clamp11(direction.y / sin);
		}

		/// <summary>
		/// Returns the cosine squared value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float Cosine2Phi(in Float3 direction)
		{
			float sin2 = Sine2(direction);
			if (sin2 == 0f) return 1f;
			return FastMath.Clamp01(direction.x * direction.x / sin2);
		}

		/// <summary>
		/// Returns the sine squared value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float Sine2Phi(in Float3 direction)
		{
			float sin2 = Sine2(direction);
			if (sin2 == 0f) return 0f;
			return FastMath.Clamp01(direction.y * direction.y / sin2);
		}

		/// <summary>
		/// Returns whether the local directions <paramref name="local0"/>
		/// and <paramref name="local1"/> are in the same hemisphere.
		/// </summary>
		public static bool SameHemisphere(in Float3 local0, in Float3 local1) => local0.z * local1.z > 0f;
	}
}