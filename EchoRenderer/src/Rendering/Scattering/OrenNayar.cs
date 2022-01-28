using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
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

		public void Reset(in Float3 newReflectance, float newSigma)
		{
			reflectance = newReflectance;
			sigma = newSigma;

			float sigma2 = sigma * sigma;

			a = 1f - sigma2 / 2f / (sigma2 + 0.33f);
			b = 0.45f * sigma2 / (sigma2 + 0.09f);
		}

		Float3 reflectance;
		float sigma;

		float a;
		float b;

		public override Float3 Evaluate(in Float3 outgoing, in Float3 incident)
		{
			float sinO = SineP(outgoing);
			float sinI = SineP(incident);

			float cosMax = 0f;

			if (!FastMath.AlmostZero(sinO) && !FastMath.AlmostZero(sinI))
			{
				float cos = CosineT(outgoing) * CosineT(incident);
				float sin = SineT(outgoing) * SineT(incident);
				cosMax = Math.Max(cos + sin, 0f);
			}

			float cosO = AbsoluteCosineP(outgoing);
			float cosI = AbsoluteCosineP(incident);

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

			return 1f / Scalars.PI * (a + b * cosMax * sinA * tanB) * reflectance;
		}
	}
}