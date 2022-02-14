using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

/// <summary>
/// The base class for either a bidirectional reflectance or transmittance distribution function.
/// NOTE: all directions in this class is in local space, meaning <see cref="Float3.forward"/> is
/// the surface normal at our point of interest, and incident and outgoing directions should also
/// point away from that point.
/// </summary>
public abstract class BxDF
{
	protected BxDF(FunctionType type) => this.type = type;

	public readonly FunctionType type;

	/// <summary>
	/// Evaluates and returns the value of this <see cref="BxDF"/> from two pairs of
	/// local directions, <paramref name="incident"/> and <paramref name="outgoing"/>.
	/// </summary>
	public abstract Float3 Evaluate(in Float3 outgoing, in Float3 incident);

	/// <summary>
	/// Returns the probability density function (pdf) for a given pair of local
	/// <paramref name="outgoing"/> and <paramref name="incident"/> directions.
	/// </summary>
	public virtual float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (!SameHemisphere(outgoing, incident)) return 0f;
		return AbsoluteCosineP(incident) * (1f / Scalars.PI);
	}

	/// <summary>
	/// Selects a local output <paramref name="incident"/> direction from <paramref name="distro"/>, evaluates the value of
	/// this <see cref="BxDF"/> from the two local directions, and outputs the probability density to <paramref name="pdf"/>.
	/// </summary>
	public virtual Float3 Sample(in Float3 outgoing, Distro2 distro, out Float3 incident, out float pdf)
	{
		incident = distro.CosineHemisphere;
		if (outgoing.z < 0f) incident = new Float3(incident.x, incident.y, -incident.z);

		pdf = ProbabilityDensity(outgoing, incident);
		return Evaluate(outgoing, incident);
	}

	/// <summary>
	/// Returns the hemispherical-directional reflectance, the total reflectance in direction
	/// <paramref name="outgoing"/> due to a constant illumination over the doming hemisphere
	/// </summary>
	public virtual Float3 GetReflectance(in Float3 outgoing, ReadOnlySpan<Distro2> distros)
	{
		Float3 result = Float3.zero;

		foreach (ref readonly Distro2 distro in distros)
		{
			Float3 sampled = Sample(outgoing, distro, out Float3 incident, out float pdf);
			if (FastMath.Positive(pdf)) result += sampled * AbsoluteCosineP(incident) / pdf;
		}

		return result / distros.Length;
	}

	/// <summary>
	/// Returns the hemispherical-hemispherical reflectance, the fraction of incident light
	/// reflected when the amount of incident light is constant across all directions
	/// </summary>
	public virtual Float3 GetReflectance(ReadOnlySpan<Distro2> distros0, ReadOnlySpan<Distro2> distros1)
	{
		Float3 result = Float3.zero;
		int length = distros0.Length;

		Assert.AreEqual(length, distros1.Length);
		for (int i = 0; i < distros0.Length; i++)
		{
			ref readonly Distro2 distro0 = ref distros0[i];
			ref readonly Distro2 distro1 = ref distros1[i];

			Float3 outgoing = distro0.UniformHemisphere;
			Float3 sampled = Sample(outgoing, distro1, out Float3 incident, out float pdf);

			pdf *= Distro2.UniformHemispherePDF;

			if (FastMath.Positive(pdf)) result += sampled * FastMath.Abs(CosineP(outgoing) * CosineP(incident)) / pdf;
		}

		return result / length;
	}

	/// <summary>
	/// Returns the cosine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP(in Float3 direction) => direction.z;

	/// <summary>
	/// Returns the cosine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP2(in Float3 direction) => direction.z * direction.z;

	/// <summary>
	/// Returns the absolute cosine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float AbsoluteCosineP(in Float3 direction) => FastMath.Abs(direction.z);

	/// <summary>
	/// Returns the sine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineP2(in Float3 direction) => FastMath.Max0(1f - CosineP2(direction));

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
		return FastMath.Clamp11(direction.x / sin);
	}

	/// <summary>
	/// Returns the sine value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT(in Float3 direction)
	{
		float sin = SineP(direction);
		if (sin == 0f) return 0f;
		return FastMath.Clamp11(direction.y / sin);
	}

	/// <summary>
	/// Returns the cosine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineT2(in Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (sin2 == 0f) return 1f;
		return FastMath.Clamp01(direction.x * direction.x / sin2);
	}

	/// <summary>
	/// Returns the sine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT2(in Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (sin2 == 0f) return 0f;
		return FastMath.Clamp01(direction.y * direction.y / sin2);
	}

	/// <summary>
	/// Returns whether the local directions <paramref name="local0"/> and <paramref name="local1"/> are in the same hemisphere.
	/// </summary>
	public static bool SameHemisphere(in Float3 local0, in Float3 local1) => local0.z * local1.z > 0f;
}