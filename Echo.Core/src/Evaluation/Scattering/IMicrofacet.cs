using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Evaluation.Scattering;

using static BxDF;

/// <summary>
/// The base class that models microfacet distribution functions.
/// </summary>
public interface IMicrofacet
{
	/// <summary>
	/// Returns the differential area of the microfacet faces oriented along <paramref name="normal"/>.
	/// </summary>
	float DifferentialArea(in Float3 normal);

	/// <summary>
	/// Returns the invisible microfacet area per visible microfacet area
	/// in <paramref name="direction"/> by sampling an auxiliary function.
	/// </summary>
	float MaskingShadowingVisible(in Float3 direction);

	/// <summary>
	/// Calculates the ratio of the projected area of invisible over visible microfacet faces.
	/// </summary>
	/// <param name="direction">The direction towards which the faces are projecting towards.</param>
	/// <returns>The area of the shadowed microfacet (blocked by others) over the area of the visible microfacet.</returns>
	float ShadowingRatio(in Float3 direction);

	/// <summary>
	/// Calculates the fraction of visible microfacet faces over all microfacet faces from a direction
	/// </summary>
	/// <param name="direction">The direction towards which the faces are projecting towards.</param>
	/// <returns>The calculated fraction, between 0 and 1.</returns>
	sealed float VisibleRatio(in Float3 direction) => 1f / (1f + MaskingShadowingVisible(direction));

	/// <summary>
	/// Returns the alpha value mapped from an artistic <paramref name="roughness"/> value between zero to one.
	/// https://github.com/mmp/pbrt-v3/blob/aaa552a4b9cbf9dccb71450f47b268e0ed6370e2/src/core/microfacet.h#L83
	/// </summary>
	protected static float GetAlpha(float roughness)
	{
		float a = MathF.Log(Math.Max(roughness, FastMath.Epsilon));
		float a2 = a * a;

		//https://www.desmos.com/calculator/wvossaascu
		return 1.62142f + 0.819955f * a + 0.1734f * a2 + 0.0171201f * a2 * a + 0.000640711f * a2 * a2;
	}
}

/// <summary>
/// A Gaussian distribution based microfacet model from Beckmann and Spizzichino (1963).
/// </summary>
public readonly struct BeckmannSpizzichinoMicrofacet : IMicrofacet
{
	public BeckmannSpizzichinoMicrofacet(float alphaX, float alphaY) => alpha = new Float2(alphaX, alphaY);

	readonly Float2 alpha;

	public float DifferentialArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(normal) / cos2;

		Float2 theta = new Float2(CosineT2(normal), SineT2(normal));
		float exp = MathF.Exp(-tan2 * (theta / (alpha * alpha)).Sum);
		return exp / (Scalars.Pi * alpha.Product * cos2 * cos2);
	}

	public float MaskingShadowingVisible(in Float3 direction)
	{
		float tan = Math.Abs(TangentP(direction));
		if (float.IsInfinity(tan)) return 0f;

		Float2 phi = new Float2(CosineT2(direction), SineT2(direction));
		float a = 1f / (MathF.Sqrt((phi * alpha * alpha).Sum) * tan);

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

	public float DifferentialArea(in Float3 normal)
	{
		float cos2 = CosineP2(normal);
		if (FastMath.AlmostZero(cos2)) return 0f;
		float tan2 = SineP2(normal) / cos2;

		Float2 theta = new Float2(CosineT2(normal), SineT2(normal));
		float sum = cos2 + cos2 * tan2 * (theta / (alpha * alpha)).Sum;
		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	public float MaskingShadowingVisible(in Float3 direction) => throw new NotImplementedException();
}