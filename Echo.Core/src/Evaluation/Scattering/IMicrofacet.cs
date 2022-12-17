using System;
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
	/// Calculates the alpha value for an <see cref="IMicrofacet"/>.
	/// </summary>
	/// <param name="roughness">An artistic value between zero and one to be mapped to the alpha value. This value will be clamped.</param>
	/// <param name="specular">Outputs whether the alpha value is too small and the model should be considered as a delta distribution.</param>
	/// <returns>The alpha value to be used with standard <see cref="IMicrofacet"/> models.</returns>
	public static float GetAlpha(float roughness, out bool specular)
	{
		roughness = FastMath.Clamp01(roughness);

		const float Threshold = 0.0001f;
		float alpha = roughness * roughness;
		specular = alpha < Threshold;
		return specular ? Threshold : alpha;
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
	public float ProjectedArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;

		Float2 theta2 = new Float2(CosineT2(normal), SineT2(normal));
		float sum = (theta2 / (alpha * alpha)).Sum * SineP2(normal) + cos2;
		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	/// <inheritdoc/>
	public float ShadowingRatio(in Float3 direction)
	{
		float cos2 = CosineP2(direction);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(direction) / cos2;

		Float2 theta2 = new Float2(CosineT2(direction), SineT2(direction));
		float alpha2Tan2 = (alpha * alpha * theta2).Sum * tan2;
		return FastMath.Sqrt0(1f + alpha2Tan2) / 2f - 0.5f;
	}

	/// <inheritdoc/>
	/// Implementation based on
	/// A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals [Heitz 2017].
	public Float3 Sample(in Float3 outgoing, Sample2D sample)
	{
		Float3 scaled = new Float3
		(
			outgoing.X * alpha.X,
			outgoing.Y * alpha.Y,
			outgoing.Z
		).Normalized;

		if (scaled.Z < 0f) scaled = -scaled;

		float threshold = 1f / (1f + scaled.Z);
		float radius = FastMath.Sqrt0(sample.x);
		float theta = sample.y < threshold ? sample.y / threshold
			: 1f + (sample.y - threshold) / (1f - threshold);

		FastMath.SinCos(theta * Scalars.Pi, out float sin, out float cos);

		float pointX = radius * sin;
		float pointY = radius * cos;

		if (sample.y >= threshold) pointY *= scaled.Z;
		float pointZ = FastMath.Sqrt0(1f - new Float2(pointX, pointY).SquaredMagnitude);

		Float3 point = new Float3(pointX, pointY, pointZ);
		var transform = new OrthonormalTransform(scaled);
		Float3 transformed = transform.LocalToWorld(point);

		return new Float3
		(
			transformed.X * alpha.X,
			transformed.Y * alpha.Y,
			FastMath.Max(transformed.Z, FastMath.Epsilon)
		).Normalized;
	}
}

public readonly struct PBRTv4TrowbridgeReitzMicrofacet : IMicrofacet
{
	public PBRTv4TrowbridgeReitzMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	static float Sqr(float value) => value * value;

	/// <inheritdoc/>
	public float ProjectedArea(in Float3 normal)
	{
		float tan2Theta = TangentP2(normal);
		if (float.IsInfinity(tan2Theta))
			return 0;
		float cos4Theta = Sqr(CosineP2(normal));
		if (cos4Theta < 1e-16f)
			return 0;
		float e = tan2Theta * (Sqr(CosineT(normal) / alpha.X) + Sqr(SineT(normal) / alpha.Y));
		return 1 / (Scalars.Pi * alpha.X * alpha.Y * cos4Theta * Sqr(1 + e));
	}

	/// <inheritdoc/>
	public float ShadowingRatio(in Float3 direction)
	{
		float tan2Theta = TangentP2(direction);
		if (float.IsInfinity(tan2Theta))
			return 0;
		float alpha2 = Sqr(CosineT(direction) * alpha.X) + Sqr(SineT(direction) * alpha.Y);
		return (MathF.Sqrt(1 + alpha2 * tan2Theta) - 1) / 2;
	}

	/// <inheritdoc/>
	/// Implementation based on
	/// A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals [Heitz 2017].
	public Float3 Sample(in Float3 outgoing, Sample2D sample)
	{
		// Transform _w_ to hemispherical configuration
		Float3 wh = new Float3(alpha.X * outgoing.X, alpha.Y * outgoing.Y, outgoing.Z).Normalized;
		if (wh.Z < 0)
			wh = -wh;

		// Find orthonormal basis for visible normal sampling
		Float3 T1 = (wh.Z < 0.99999f) ? Cross(new Float3(0, 0, 1), wh).Normalized
			: new Float3(1, 0, 0);
		Float3 T2 = Cross(wh, T1);

		// Generate uniformly distributed points on the unit disk
		Float2 p = SampleUniformDiskPolar(sample);

		// Warp hemispherical projection for visible normal sampling
		float h = MathF.Sqrt(1 - Sqr(p.X));
		p = new Float2(p.X, Scalars.Lerp(h, p.Y, (1 + wh.Z) / 2));

		// Reproject to hemisphere and transform normal to ellipsoid configuration
		float pz = MathF.Sqrt(Math.Max(0, 1 - p.SquaredMagnitude));
		Float3 nh = p.X * T1 + p.Y * T2 + pz * wh;
		return new Float3(alpha.X * nh.X, alpha.Y * nh.Y, Math.Max(1e-6f, nh.Z)).Normalized;

		static float DifferenceOfProducts(float a, float b, float c, float d)
		{
			float cd = c * d;
			float differenceOfProducts = MathF.FusedMultiplyAdd(a, b, -cd);
			float error = MathF.FusedMultiplyAdd(-c, d, cd);
			return differenceOfProducts + error;
		}

		static Float3 Cross(Float3 v, Float3 w)
		{
			return new Float3(
				DifferenceOfProducts(v.Y, w.Z, v.Z, w.Y),
				DifferenceOfProducts(v.Z, w.X, v.X, w.Z),
				DifferenceOfProducts(v.X, w.Y, v.Y, w.X));
		}

		static Float2 SampleUniformDiskPolar(Sample2D sample)
		{
			float r = MathF.Sqrt(sample.x);
			float theta = 2 * Scalars.Pi * sample.y;
			return new Float2(r * MathF.Cos(theta), r * MathF.Sin(theta));
		}
	}
}