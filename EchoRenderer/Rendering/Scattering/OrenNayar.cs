using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	public class OrenNayar : BidirectionalDistributionFunction
	{
		public OrenNayar() : base
		(
			BidirectionalDistributionFunctionType.reflection |
			BidirectionalDistributionFunctionType.diffuse
		) { }

		public void Reset(in Float3 newReflectance, float newSigma)
		{
			reflectance = newReflectance;
			sigma       = newSigma * Scalars.DegreeToRadian;

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

			float cos = 0f;
			if (!sinO.AlmostEquals() && !sinI.AlmostEquals())
			{
				float sinPhiO = SinePhi(outgoing);
				float sinPhiI = SinePhi(incident);

				float cosPhiO = CosinePhi(outgoing);
				float cosPhiI = CosinePhi(outgoing);
			}

			throw new NotImplementedException();
		}
	}
}