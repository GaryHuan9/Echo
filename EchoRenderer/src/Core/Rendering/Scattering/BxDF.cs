using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

/// <summary>
/// The base class for either a bidirectional reflectance or transmittance distribution function.
/// NOTE: unless specifically indicated, all directions in this class is in local space, meaning
/// <see cref="Float3.Forward"/> is the surface normal at our point of interest, and incident and
/// outgoing directions point away from that point.
/// </summary>
public abstract class BxDF
{
	protected BxDF(FunctionType type) => this.type = type;

	public readonly FunctionType type;

	/// <summary>
	/// Evaluates and returns the value of this <see cref="BxDF"/> from two pairs of <see cref="BxDF"/> local directions,
	/// the leaving <paramref name="incident"/> direction and the returning <paramref name="outgoing"/> direction.
	/// </summary>
	public abstract RGBA128 Evaluate(in Float3 outgoing, in Float3 incident);

	/// <summary>
	/// Returns the probability density function (pdf) value from two pairs of <see cref="BxDF"/> local directions,
	/// the leaving <paramref name="incident"/> direction and the returning <paramref name="outgoing"/> direction.
	/// </summary>
	public virtual float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (!SameHemisphere(outgoing, incident)) return 0f;
		return FastMath.Abs(CosineP(incident)) * (1f / Scalars.Pi);
	}

	/// <summary>
	/// Samples a leaving <paramref name="incident"/> local direction based on <paramref name="sample"/>, outputs
	/// the probability density function (pdf) value of doing so to <paramref name="pdf"/>, and returns the value
	/// of this <see cref="BxDF"/> evaluated from the returning local <paramref name="outgoing"/> direction to the
	/// <paramref name="incident"/> direction that we just sampled.
	/// </summary>
	public virtual Probable<RGBA128> Sample(in Float3 outgoing, Sample2D sample, out Float3 incident)
	{
		incident = sample.CosineHemisphere;
		if (outgoing.Z < 0f) incident = new Float3(incident.X, incident.Y, -incident.Z);

		return (Evaluate(outgoing, incident), ProbabilityDensity(outgoing, incident));
	}

	/// <summary>
	/// Returns the hemispherical-directional reflectance, the total reflectance in direction
	/// <paramref name="outgoing"/> due to a constant illumination over the doming hemisphere
	/// </summary>
	public virtual RGBA128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples)
	{
		var result = RGBA128.Zero;

		foreach (ref readonly Sample2D sample in samples)
		{
			Probable<RGBA128> sampled = Sample(outgoing, sample, out Float3 incident);

			if (sampled.IsZero | sampled.content.IsZero) continue;

			result += sampled.content * FastMath.Abs(CosineP(incident)) / sampled.pdf;
		}

		return result / samples.Length;
	}

	/// <summary>
	/// Returns the hemispherical-hemispherical reflectance, the fraction of incident light
	/// reflected when the amount of incident light is constant across all directions
	/// </summary>
	public virtual RGBA128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1)
	{
		Assert.AreEqual(samples0.Length, samples1.Length);

		var result = RGBA128.Zero;

		for (int i = 0; i < samples0.Length; i++)
		{
			Float3 outgoing = samples0[i].UniformHemisphere;
			Probable<RGBA128> sampled = Sample(outgoing, samples1[i], out Float3 incident);

			if (sampled.IsZero | sampled.content.IsZero) continue;

			result += sampled.content * FastMath.Abs(CosineP(outgoing) * CosineP(incident))
					/ (sampled.pdf * Sample2D.UniformHemispherePdf);
		}

		return result / samples0.Length;
	}

	/// <summary>
	/// Returns the local surface normal that lies in the same hemisphere as <paramref name="direction"/>.
	/// </summary>
	public static Float3 Normal(in Float3 direction) => direction.Z < 0f ? Float3.Backward : Float3.Forward;

	/// <summary>
	/// Returns the cosine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP(in Float3 direction) => direction.Z;

	/// <summary>
	/// Returns the cosine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP2(in Float3 direction) => direction.Z * direction.Z;

	/// <summary>
	/// Returns the sine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineP2(in Float3 direction) => FastMath.OneMinus2(direction.Z);

	/// <summary>
	/// Returns the sine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineP(in Float3 direction) => FastMath.Identity(CosineP(direction));

	/// <summary>
	/// Returns the tangent value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float TangentP(in Float3 direction) => SineP(direction) / CosineP(direction);

	/// <summary>
	/// Returns the tangent squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float TangentP2(in Float3 direction) => SineP2(direction) / CosineP2(direction);

	/// <summary>
	/// Returns the cosine value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineT(in Float3 direction)
	{
		float sin = SineP(direction);
		if (sin == 0f) return 1f;
		return FastMath.Clamp11(direction.X / sin);
	}

	/// <summary>
	/// Returns the sine value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT(in Float3 direction)
	{
		float sin = SineP(direction);
		if (sin == 0f) return 0f;
		return FastMath.Clamp11(direction.Y / sin);
	}

	/// <summary>
	/// Returns the cosine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineT2(in Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (sin2 == 0f) return 1f;
		return FastMath.Clamp01(direction.X * direction.X / sin2);
	}

	/// <summary>
	/// Returns the sine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT2(in Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (sin2 == 0f) return 0f;
		return FastMath.Clamp01(direction.Y * direction.Y / sin2);
	}

	/// <summary>
	/// Returns whether the local directions <paramref name="local0"/> and <paramref name="local1"/> are in the same hemisphere.
	/// </summary>
	public static bool SameHemisphere(in Float3 local0, in Float3 local1) => local0.Z * local1.Z > 0f;
}