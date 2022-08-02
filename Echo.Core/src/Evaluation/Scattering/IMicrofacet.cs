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
	/// <param name="normal">The microfacet surface normal to measure against.</param>
	/// <returns>The calculated projected area.</returns>
	public float ProjectedArea(in Float3 normal);

	/// <summary>
	/// Calculates the ratio of the projected area of invisible over visible microfacet faces.
	/// </summary>
	/// <param name="direction">The direction towards which the projected face areas are calculated from.</param>
	/// <returns>The area of the shadowed microfacet (blocked by others) over the area of the visible microfacet.</returns>
	float ShadowingRatio(in Float3 direction);

	Float3 Sample(in Float3 outgoing, Sample2D sample);

	/// <summary>
	/// Calculates the fraction of visible microfacet faces over all microfacet faces from a direction.
	/// </summary>
	/// <param name="direction">The direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	sealed float Visibility(in Float3 direction) => 1f / (1f + ShadowingRatio(direction));

	/// <summary>
	/// Calculates the fraction of visible microfacet faces from two directions.
	/// </summary>
	/// <param name="outgoing">The first direction towards which the visibility is calculated.</param>
	/// <param name="incident">The second direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	sealed float Visibility(in Float3 outgoing, in Float3 incident) => 1f / (1f + ShadowingRatio(outgoing) + ShadowingRatio(incident));

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="normal"/> from <see cref="outgoing"/> with <see cref="Sample"/>.
	/// </summary>
	/// <param name="outgoing">The unit local source direction from which we hit this <see cref="IMicrofacet"/>.</param>
	/// <param name="normal">The unit local normal direction that was probabilistically selected to be sampled.</param>
	/// <returns>The probability density function (pdf) value of this selection.</returns>
	/// <seealso cref="Sample"/>
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
		return roughness * roughness;
	}
}

/// <summary>
/// A microfacet distribution model first proposed by Trowbridge and Reitz (1975).
/// </summary>
public readonly struct TrowbridgeReitzMicrofacet : IMicrofacet
{
	public TrowbridgeReitzMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	public float ProjectedArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;

		Float2 theta = new Float2(CosineT2(normal), SineT2(normal));
		float sum = (theta / (alpha * alpha)).Sum * SineP2(normal) + cos2;
		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	public float ShadowingRatio(in Float3 direction)
	{
		float cos2 = CosineP2(direction);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(direction) / cos2;

		Float2 theta = new Float2(CosineT2(direction), SineT2(direction));
		float alpha2Tan2 = (alpha * alpha * theta).Sum * tan2;
		return FastMath.Sqrt0(1f + alpha2Tan2) / 2f - 0.5f;
	}

	public Float3 Sample(in Float3 outgoing, Sample2D sample)
	{
		Float3 v = new Float3(alpha.X * outgoing.X, alpha.Y * outgoing.Y, outgoing.Z).Normalized;

		Float3 t1 = v.Z < 0.9999f ? v.Cross(Float3.Forward).Normalized : Float3.Right;
		Float3 t2 = Float3.Cross(t1, v);

		float a = 1f / (1f + v.Z);
		float r = FastMath.Sqrt0(sample.x);
		float phi = sample.y < a ? sample.y / a * Scalars.Pi : Scalars.Pi + (sample.y - a) / (1f - a) * Scalars.Pi;
		float p1 = r * MathF.Cos(phi);
		float p2 = r * MathF.Sin(phi) * (sample.y < a ? 1f : v.Z);

		Float3 n = p1 * t1 + p2 * t2 + FastMath.Sqrt0(1f - p1 * p1 - p2 * p2) * v;
		return new Float3(alpha.X * n.X, alpha.Y * n.Y, MathF.Max(0f, n.Z)).Normalized;
	}
}