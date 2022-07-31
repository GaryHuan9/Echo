using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Microfacet diffuse reflection based on Oren and Nayar (1994)
/// </summary>
public sealed class OrenNayar : BxDF
{
	public OrenNayar() : base
	(
		FunctionType.Reflective |
		FunctionType.Diffuse
	) { }

	public void Reset(float newRadian)
	{
		float sigma2 = newRadian * newRadian;

		a = 1f - sigma2 / (sigma2 + 0.33f) / 2f;
		b = 0.45f * sigma2 / (sigma2 + 0.09f);
	}

	float a;
	float b;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		float sinO = SineP(outgoing);
		float sinI = SineP(incident);

		//Calculate cosMax using trigonometric identities
		float cosMax = 0f;

		if (FastMath.Positive(sinO) && FastMath.Positive(sinI))
		{
			float cos = CosineT(outgoing) * CosineT(incident);
			float sin = SineT(outgoing) * SineT(incident);
			cosMax = FastMath.Max0(cos + sin);
		}

		//Calculate sine and tangent
		float sinA;
		float tanB;

		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		if (cosO < cosI)
		{
			sinA = sinO;
			tanB = sinI / cosI;
		}
		else
		{
			sinA = sinI;
			tanB = sinO / cosO;
		}

		return new RGB128(Scalars.PiR) * (a + b * cosMax * sinA * tanB);
	}
}