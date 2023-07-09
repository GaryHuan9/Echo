using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
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
	/// Calculates the projected differential area of the microfacet faces along a normal.
	/// </summary>
	/// <param name="normal">The microfacet surface normal to measure against.</param>
	/// <returns>The calculated projected area.</returns>
	public float ProjectedArea(Float3 normal);

	/// <summary>
	/// Calculates the ratio of the projected area of invisible over visible microfacet faces.
	/// </summary>
	/// <param name="direction">The direction towards which the projected face areas are calculated from.</param>
	/// <returns>The area of the shadowed microfacet (blocked by others) over the area of the visible microfacet.</returns>
	public float ShadowingRatio(Float3 direction);

	/// <summary>
	/// Samples this <see cref="IMicrofacet"/> and produces a normal direction that should be used.
	/// </summary>
	/// <param name="outgoing">The <see cref="BxDF"/> local direction from which light enters.</param>
	/// <param name="sample">The <see cref="Sample2D"/> to use.</param>
	/// <returns>The normal direction that was sampled to produce an incident direction.</returns>
	public Float3 Sample(Float3 outgoing, Sample2D sample);

	/// <summary>
	/// Calculates the alpha value for an <see cref="IMicrofacet"/>.
	/// </summary>
	/// <param name="roughness">An artistic value between zero and one to be mapped to the alpha value. This value will be clamped.</param>
	/// <param name="specular">Outputs whether the alpha value is too small and the model should be considered as a delta distribution.</param>
	/// <returns>The alpha value to be used with standard <see cref="IMicrofacet"/> models.</returns>
	public static float GetAlpha(float roughness, out bool specular)
	{
		roughness = FastMath.Clamp01(roughness * 0.75f); //Values too high are visually unnatural

		const float Threshold = 0.0001f;
		float alpha = roughness * roughness;
		specular = alpha < Threshold;
		return specular ? Threshold : alpha;
	}
}

public static class MicrofacetExtensions
{
	/// <summary>
	/// Calculates the fraction of visible microfacet faces over all microfacet faces from a direction.
	/// </summary>
	/// <param name="microfacet">The <see cref="IMicrofacet"/> to use.</param>
	/// <param name="direction">The direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	public static float Visibility<T>(this T microfacet, Float3 direction) where T : IMicrofacet =>
		1f / (1f + microfacet.ShadowingRatio(direction));

	/// <summary>
	/// Calculates the fraction of visible microfacet faces from two directions.
	/// </summary>
	/// <param name="microfacet">The <see cref="IMicrofacet"/> to use.</param>
	/// <param name="outgoing">The first direction towards which the visibility is calculated.</param>
	/// <param name="incident">The second direction towards which the visibility is calculated.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	public static float Visibility<T>(this T microfacet, Float3 outgoing, Float3 incident) where T : IMicrofacet =>
		1f / (1f + microfacet.ShadowingRatio(outgoing) + microfacet.ShadowingRatio(incident));

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="normal"/> from <see cref="outgoing"/> with <see cref="IMicrofacet.Sample"/>.
	/// </summary>
	/// <param name="microfacet">The <see cref="IMicrofacet"/> to use.</param>
	/// <param name="outgoing">The unit local source direction from which we hit this <see cref="IMicrofacet"/>.</param>
	/// <param name="normal">The unit local normal direction that was probabilistically selected to be sampled.</param>
	/// <returns>The probability density function (pdf) value of this selection.</returns>
	/// <seealso cref="IMicrofacet.Sample"/>
	public static float ProbabilityDensity<T>(this T microfacet, Float3 outgoing, Float3 normal) where T : IMicrofacet
	{
		float fraction = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing);
		return fraction * FastMath.Abs(outgoing.Dot(normal) / CosineP(outgoing));
	}
}

/// <summary>
/// The GGX microfacet distribution model first proposed by
/// Average irregularity representation of a rough surface for ray reflection [Trowbridge and Reitz 1975].
/// </summary>
public readonly struct TrowbridgeReitzMicrofacet : IMicrofacet
{
	public TrowbridgeReitzMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	/// <inheritdoc/>
	public float ProjectedArea(Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (!FastMath.Positive(cos2)) return 0f;

		float sum = cos2;

		if (FastMath.Positive(1f - cos2, 1E-5f))
		{
			Float2 xy = new Float2(normal.X, normal.Y);
			sum += (xy / alpha).SquaredMagnitude;
		}

		//Above is an algebraically equivalent version of the following two lines, with a few tweaks to avoid catastrophic cancellation

		// Float2 theta2 = new Float2(CosineT2(normal), SineT2(normal));
		// float sum = (theta2 / (alpha * alpha)).Sum * SineP2(normal) + cos2;

		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	/// <inheritdoc/>
	public float ShadowingRatio(Float3 direction)
	{
		float cos2 = CosineP2(direction);
		if (!FastMath.Positive(cos2)) return 0f;
		float tan2 = SineP2(direction) / cos2;

		Float2 theta2 = new Float2(CosineT2(direction), SineT2(direction));
		float alpha2Tan2 = (alpha * alpha * theta2).Sum * tan2;
		return FastMath.Sqrt0(1f + alpha2Tan2) / 2f - 0.5f;
	}

	/// <inheritdoc/>
	/// Implementation based on
	/// A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals [Heitz 2017].
	public Float3 Sample(Float3 outgoing, Sample2D sample)
	{
		//Scale direction
		Float3 scaled = new Float3
		(
			outgoing.X * alpha.X,
			outgoing.Y * alpha.Y,
			outgoing.Z
		).Normalized;

		if (scaled.Z < 0f) scaled = -scaled;

		//Get point from sample
		float threshold = 1f / (1f + scaled.Z);
		float radius = FastMath.Sqrt0(sample.x);
		float theta = sample.y < threshold ? sample.y / threshold
			: 1f + (sample.y - threshold) / (1f - threshold);

		FastMath.SinCos(theta * -Scalars.Pi, out float sin, out float cos);

		float pointX = radius * cos;
		float pointY = radius * sin;

		if (sample.y >= threshold) pointY *= scaled.Z;
		float pointZ = FastMath.Sqrt0(1f - new Float2(pointX, pointY).SquaredMagnitude);

		//Convert point back to original space
		Float3 point = new Float3(pointX, pointY, pointZ);
		var transform = new OrthonormalTransform(scaled);
		Float3 transformed = transform.ApplyForward(point);

		return new Float3
		(
			transformed.X * alpha.X,
			transformed.Y * alpha.Y,
			FastMath.Max(transformed.Z, FastMath.Epsilon)
		).Normalized;
	}
}