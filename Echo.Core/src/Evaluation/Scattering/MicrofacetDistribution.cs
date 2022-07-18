using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Evaluation.Scattering;

using static BxDF;

/// <summary>
/// The base class that models microfacet distribution functions.
/// </summary>
public abstract class MicrofacetDistribution
{
	/// <summary>
	/// Returns the differential area of the microfacet faces oriented along <paramref name="normal"/>.
	/// </summary>
	public abstract float DifferentialArea(in Float3 normal);

	/// <summary>
	/// Returns the fraction (0 to 1) of microfacet faces that are visible from <paramref name="direction"/>.
	/// Note that this returns the percentage of faces visible, out of the total amount of faces.
	/// </summary>
	public float MaskingShadowing(in Float3 direction) => 1f / (1f + MaskingShadowingVisible(direction));

	/// <summary>
	/// Returns the invisible microfacet area per visible microfacet area
	/// in <paramref name="direction"/> by sampling an auxiliary function.
	/// </summary>
	public abstract float MaskingShadowingVisible(in Float3 direction);

	/// <summary>
	/// Returns the alpha value mapped from an artistic <paramref name="roughness"/> value between zero to one.
	/// https://github.com/mmp/pbrt-v3/blob/aaa552a4b9cbf9dccb71450f47b268e0ed6370e2/src/core/microfacet.h#L83
	/// </summary>
	protected static float GetAlpha(float roughness)
	{
		float a = MathF.Log(Math.Max(roughness, FastMath.Epsilon));
		float a2 = a * a;

		return 1.62142f + 0.819955f * a + 0.1734f * a2 + 0.0171201f * a2 * a + 0.000640711f * a2 * a2;
	}
}

/// <summary>
/// A Gaussian distribution based microfacet model from Beckmann and Spizzichino (1963).
/// </summary>
public class BeckmannDistribution : MicrofacetDistribution
{
	public void Reset(Float2 newAlpha)
	{
		alpha = newAlpha.Max((Float2)FastMath.Epsilon);
	}

	Float2 alpha;

	public override float DifferentialArea(in Float3 normal)
	{
		float tan2 = TangentP2(normal);
		if (float.IsInfinity(tan2)) return 0f;
		float cos2 = CosineP2(normal);

		Float2 phi = new Float2(CosineT2(normal), SineT2(normal));
		float exp = MathF.Exp(-tan2 * (phi / (alpha * alpha)).Sum);
		return exp / (Scalars.Pi * alpha.Product * cos2 * cos2);
	}

	public override float MaskingShadowingVisible(in Float3 direction)
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
public class TrowbridgeReitzDistribution : MicrofacetDistribution
{
	public void Reset(Float2 newAlpha)
	{
		alpha = newAlpha.Max((Float2)FastMath.Epsilon);
	}

	Float2 alpha;

	public override float DifferentialArea(in Float3 normal)
	{
		float tan2 = TangentP2(normal);
		if (float.IsInfinity(tan2)) return 0f;
		float cos2 = CosineP2(normal);

		Float2 phi = new Float2(CosineT2(normal), SineT2(normal));
		float sum = cos2 + cos2 * tan2 * (phi / (alpha * alpha)).Sum;
		return 1f / (sum * sum * alpha.Product * Scalars.Pi);
	}

	public override float MaskingShadowingVisible(in Float3 direction) => throw new NotImplementedException();
}