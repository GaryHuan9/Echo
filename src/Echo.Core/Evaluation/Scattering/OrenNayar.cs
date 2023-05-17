using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Microfacet diffuse reflection model originally proposed by
/// Generalization of Lambert's reflectance model [Oren and Nayar 1994].
/// Implementation based on: https://mimosa-pudica.net/improved-oren-nayar.html
/// </summary>
public sealed class OrenNayar : LambertianReflection
{
	public void Reset(float newRoughness)
	{
		Ensure.IsTrue(newRoughness is >= 0f and <= 1f);

		a = 1f / FastMath.FMA(Scalars.Pi / 2f - 2f / 3f, newRoughness, Scalars.Pi);
		b = a * newRoughness;
	}

	float a;
	float b;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;

		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		float s = outgoing.Dot(incident) - cosO * cosI;
		if (FastMath.Positive(s)) s /= FastMath.Max(cosO, cosI);
		return new RGB128(a + b * s);
	}
}