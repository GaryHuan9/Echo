using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;

namespace Echo.Core.Evaluation.Scattering;

using static BxDF;

/// <summary>
/// The base class that models microfacet distribution functions.
/// </summary>
public interface IMicrofacet
{
	/// <summary>
	/// Calculates the projected area of the microfacet faces along a normal.
	/// </summary>
	/// <param name="normal">The surface normal to measure against.</param>
	/// <returns>The calculated projected area.</returns>
	float ProjectedArea(in Float3 normal);

	/// <summary>
	/// Calculates the ratio of the projected area of invisible over visible microfacet faces.
	/// </summary>
	/// <param name="outgoing">The direction towards which the projected face areas are calculated from.</param>
	/// <returns>The area of the shadowed microfacet (blocked by others) over the area of the visible microfacet.</returns>
	float ShadowingRatio(in Float3 outgoing);

	Float3 Sample(in Float3 direction, Sample2D sample);

	/// <summary>
	/// Calculates the fraction of visible microfacet faces over all microfacet faces from a direction.
	/// </summary>
	/// <param name="outgoing">The direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	sealed float Visibility(in Float3 outgoing) => 1f / (1f + ShadowingRatio(outgoing));

	/// <summary>
	/// Calculates the fraction of visible microfacet faces from two directions.
	/// </summary>
	/// <param name="outgoing">The first direction towards which the visibility is calculated.</param>
	/// <param name="incident">The second direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	sealed float Visibility(in Float3 outgoing, in Float3 incident) => 1f / (1f + ShadowingRatio(outgoing) + ShadowingRatio(incident));

	sealed float ProbabilityDensity(in Float3 outgoing, in Float3 normal)
	{
		float fraction = ProjectedArea(normal) * Visibility(outgoing);
		return fraction * FastMath.Abs(outgoing.Dot(normal) / CosineP(outgoing));
	}

	/// <summary>
	/// Returns the alpha value mapped from an artistic <paramref name="roughness"/> value between zero to one.
	/// </summary>
	public static float GetAlpha(float roughness)
	{
		Ensure.IsTrue(roughness >= 0f);
		Ensure.IsTrue(roughness <= 1f);
		return Scalars.Phi * FastMath.Sqrt0(roughness);
	}
}

/// <summary>
/// A Gaussian distribution based microfacet model from Beckmann and Spizzichino (1963).
/// </summary>
public readonly struct BeckmannSpizzichinoMicrofacet : IMicrofacet
{
	public BeckmannSpizzichinoMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	public float ProjectedArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(normal) / cos2;

		Float2 theta = new Float2(CosineT2(normal), SineT2(normal));
		float exp = MathF.Exp(-tan2 * (theta / (alpha * alpha)).Sum);
		return exp / (Scalars.Pi * alpha.Product * cos2 * cos2);
	}

	public float ShadowingRatio(in Float3 outgoing)
	{
		float cos = CosineP(outgoing);
		if (FastMath.AlmostZero(cos)) return 0f;
		float tan = SineP(outgoing) / cos;

		Float2 phi = new Float2(CosineT2(outgoing), SineT2(outgoing));
		float interpolated = FastMath.Sqrt0((phi * alpha * alpha).Sum);
		float x = 1f / (interpolated * FastMath.Abs(tan));

		if (x >= 1.6f) return 0f;

		//Polynomial approximation checkout this article: http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html
		float numerator = 1f - 1.259f * x + 0.396f * x * x;
		float denominator = 3.535f * x + 2.181f * x * x;
		return numerator / denominator;
	}

	public Float3 Sample(in Float3 direction, Sample2D sample)
	{
		//Use and cite: https://hal.inria.fr/hal-00996995v2/file/supplemental1.pdf
		throw new NotImplementedException();
	}
}

/// <summary>
/// A microfacet distribution model from Trowbridge and Reitz (1975).
/// </summary>
public readonly struct TrowbridgeReitzMicrofacet : IMicrofacet
{
	public TrowbridgeReitzMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	public float ProjectedArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(normal) / cos2;

		Float2 theta = new Float2(CosineT2(normal), SineT2(normal));
		float sum = cos2 + cos2 * tan2 * (theta / (alpha * alpha)).Sum;
		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	public float ShadowingRatio(in Float3 outgoing)
	{
		float cos2 = CosineP2(outgoing);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(outgoing) / cos2;

		Float2 phi = new Float2(CosineT2(outgoing), SineT2(outgoing));
		float interpolated = FastMath.Sqrt0((phi * alpha * alpha).Sum);

		float product = interpolated * interpolated * tan2;
		return FastMath.Sqrt0(1f + product) / 2f - 0.5f;
	}

	public Float3 Sample(in Float3 direction, Sample2D sample)
	{
		//Use and cite: https://hal.inria.fr/hal-00996995v2/file/supplemental1.pdf
		throw new NotImplementedException();
	}
}