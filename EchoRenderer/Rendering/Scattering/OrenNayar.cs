using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// Microfacet diffuse reflection based on Oren and Nayer (1994)
	/// </summary>
	public class OrenNayar : BidirectionalDistributionFunction
	{
		public OrenNayar() : base
		(
			FunctionType.reflection |
			FunctionType.diffuse
		) { }

		public void Reset(in Float3 newReflectance, float newSigma)
		{
			reflectance = newReflectance;
			sigma = newSigma * Scalars.DegreeToRadian;

			float sigma2 = sigma * sigma;

			a = 1f - sigma2 / 2f / (sigma2 + 0.33f);
			b = 0.45f * sigma2 / (sigma2 + 0.09f);
		}

		Float3 reflectance;
		float  sigma;

		float a;
		float b;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident)
		{
			float sinO = Sine(outgoing);
			float sinI = Sine(incident);

			float cosMax = 0f;

			if (!sinO.AlmostEquals() && !sinI.AlmostEquals())
			{
				float cos = CosinePhi(outgoing) * CosinePhi(incident);
				float sin = SinePhi(outgoing) * SinePhi(incident);
				cosMax = Math.Max(cos + sin, 0f);
			}

			float cosO = AbsoluteCosine(outgoing);
			float cosI = AbsoluteCosine(incident);

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