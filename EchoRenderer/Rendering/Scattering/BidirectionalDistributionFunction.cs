using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// The base class to either bidirectional reflectance distribution function or bidirectional transmittance distribution function.
	/// </summary>
	public abstract class BidirectionalDistributionFunction
	{
		protected BidirectionalDistributionFunction(BidirectionalDistributionFunctionType type) => bsdfType = type;

		readonly BidirectionalDistributionFunctionType bsdfType;

		public bool MatchType(BidirectionalDistributionFunctionType type) => (bsdfType & type) == type;
		public bool HasType(BidirectionalDistributionFunctionType   type) => (bsdfType & type) != 0;

		/// <summary>
		/// Samples and returns the value of the distribution function from two pairs of directions,
		/// <paramref name="incident"/> and <paramref name="outgoing"/>.
		/// </summary>
		public abstract Float3 Sample(in Float3 outgoing, in Float3 incident);

		/// <summary>
		/// Samples and returns the value of the distribution function from <paramref name="outgoing"/>, and outputs
		/// the scattering direction to <paramref name="incident"/>. Used by delta distributions (eg. perfect specular)
		/// </summary>
		public virtual Float3 Sample(in Float3 outgoing, out Float3 incident, in Float2 sample, out float pdf, ref BidirectionalDistributionFunctionType type) => throw new NotImplementedException();

		/// <summary>
		/// Returns the hemispherical-directional reflectance, the total reflectance in direction
		/// <paramref name="outLocal"/> due to a constant illumination over the doming hemisphere
		/// </summary>
		public virtual Float3 GetReflectance(in Float3 outLocal, ReadOnlySpan<Float2> samples) => throw new NotImplementedException();

		/// <summary>
		/// Returns the hemispherical-hemispherical reflectance, the fraction of incident light
		/// reflected when the amount of incident light is constant across all directions
		/// </summary>
		public virtual Float3 GetReflectance(ReadOnlySpan<Float2> samples0, ReadOnlySpan<Float2> samples1) => throw new NotImplementedException();

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
		public static float AbsoluteCosine(in Float3 direction) => Math.Abs(direction.z);

		/// <summary>
		/// Returns the sine squared value of the local <paramref name="direction"/> with the normal.
		/// </summary>
		public static float Sine2(in Float3 direction) => Math.Max(1f - Cosine2(direction), 0f);

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
			return (direction.x / sin).Clamp(-1f);
		}

		/// <summary>
		/// Returns the sine value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float SinePhi(in Float3 direction)
		{
			float sin = Sine(direction);
			if (sin == 0f) return 0f;
			return (direction.y / sin).Clamp(-1f);
		}

		/// <summary>
		/// Returns the cosine squared value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float Cosine2Phi(in Float3 direction)
		{
			float sin2 = Sine2(direction);
			if (sin2 == 0f) return 1f;
			return (direction.x * direction.x / sin2).Clamp();
		}

		/// <summary>
		/// Returns the sine squared value of the local <paramref name="direction"/>
		/// with the tangent direction in the horizontal/flat angle phi.
		/// </summary>
		public static float Sine2Phi(in Float3 direction)
		{
			float sin2 = Sine2(direction);
			if (sin2 == 0f) return 0f;
			return (direction.y * direction.y / sin2).Clamp();
		}
	}
}