using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	public readonly struct FresnelDielectric
	{
		public FresnelDielectric(float newEtaI, float newEtaT)
		{
			fresnelEtaI = newEtaI;
			fresnelEtaT = newEtaT;
		}

		readonly float fresnelEtaI;
		readonly float fresnelEtaT;

		public Float3 Evaluate(float cosI)
		{
			float etaI = fresnelEtaI;
			float etaT = fresnelEtaT;

			cosI = FastMath.Clamp11(cosI);

			//Swap indices of refraction if needed
			if (cosI < 0f)
			{
				CodeHelper.Swap(ref etaI, ref etaT);
				cosI = -cosI;
			}

			//Apply Snell's law
			float sinI = FastMath.Identity(cosI);
			float sinT = etaI / etaT * sinI;
			if (sinT >= 1f) return Float3.one; //Total internal reflection
			float cosT = FastMath.Identity(sinT);

			//Fresnel equation
			float ti = etaT * cosI;
			float it = etaI * cosT;

			float ii = etaI * cosI;
			float tt = etaT * cosT;

			float Rs = (ti - it) / (ti + it);
			float Rp = (ii - tt) / (ii + tt);

			return (Float3)((Rs * Rs + Rp * Rp) / 2f);
		}
	}

	public readonly struct FresnelConductor
	{
		public FresnelConductor(in Float3 newEtaI, in Float3 newEtaT, in Float3 newAbsorption)
		{
			Float3 inverse = 1f / newEtaI;

			eta2 = newEtaT * inverse;
			etaK2 = newAbsorption * inverse;

			eta2 *= eta2;
			etaK2 *= etaK2;
		}

		readonly Float3 eta2;
		readonly Float3 etaK2;

		public Float3 Evaluate(float cosI) //https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
		{
			cosI = Math.Min(Math.Abs(cosI), 1f);

			float cosI2 = cosI * cosI;
			float sinI2 = 1f - cosI2;

			Float3 gamma = eta2 - etaK2 - (Float3)sinI2;
			Float3 sum = Sqrt(gamma * gamma + 4f * eta2 * etaK2);

			Float3 term0 = sum + (Float3)cosI2;
			Float3 term1 = cosI * Scalars.Sqrt2 * Sqrt(sum + gamma);

			Float3 term2 = cosI2 * sum + (Float3)(sinI2 * sinI2);
			Float3 term3 = term1 * sinI2;

			Float3 Rs = (term0 - term1) / (term0 + term1);
			Float3 Rp = (term2 - term3) / (term2 + term3);

			return (Rs + Rs * Rp) / 2f;
		}

		static Float3 Sqrt(in Float3 value) => new(MathF.Sqrt(value.x), MathF.Sqrt(value.y), MathF.Sqrt(value.z));
	}
}