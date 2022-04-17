using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Textures.Colors;

namespace EchoRenderer.Core.Evaluation.Scattering;

/// <summary>
/// Microfacet diffuse reflection based on Oren and Nayer (1994)
/// </summary>
public class OrenNayar : BxDF
{
	public OrenNayar() : base
	(
		FunctionType.reflective |
		FunctionType.diffuse
	) { }

	public void Reset(in RGB128 newReflectance, float newSigma)
	{
		reflectance = newReflectance;
		sigma = newSigma;

		float sigma2 = sigma * sigma;

		a = 1f - sigma2 / 2f / (sigma2 + 0.33f);
		b = 0.45f * sigma2 / (sigma2 + 0.09f);
	}

	RGB128 reflectance;
	float sigma;

	float a;
	float b;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		float sinO = SineP(outgoing);
		float sinI = SineP(incident);

		float cosMax = 0f;

		if (!FastMath.AlmostZero(sinO) & !FastMath.AlmostZero(sinI))
		{
			float cos = CosineT(outgoing) * CosineT(incident);
			float sin = SineT(outgoing) * SineT(incident);
			cosMax = Math.Max(cos + sin, 0f);
		}

		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		float sinA;
		float tanB;

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

		return reflectance * (a + b * cosMax * sinA * tanB) * Scalars.PiR;
	}
}