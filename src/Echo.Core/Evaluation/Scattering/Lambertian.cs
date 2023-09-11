using System;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Perfectly uniform Lambertian diffuse reflection.
/// </summary>
public class LambertianReflection : BxDF
{
	public LambertianReflection() : base(FunctionType.Reflective | FunctionType.Diffuse) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;
		return new RGB128(Scalars.PiR);
	}

	public sealed override float ProbabilityDensity(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;
		return FastMath.Abs(CosineP(incident)) * Scalars.PiR;
	}

	public sealed override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		incident = sample.CosineHemisphere;
		float pdf = CosineP(incident) * Scalars.PiR;

		if (outgoing.Z < 0f) incident = Utility.NegateZ(incident);
		return (Evaluate(outgoing, incident), pdf);
	}
}

/// <summary>
/// Perfectly uniform Lambertian diffuse transmission.
/// </summary>
public sealed class LambertianTransmission : BxDF
{
	public LambertianTransmission() : base(FunctionType.Diffuse | FunctionType.Transmissive) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return RGB128.Black;
		return new RGB128(Scalars.PiR);
	}

	public override float ProbabilityDensity(Float3 outgoing, Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return 0f;
		return FastMath.Abs(CosineP(incident)) * Scalars.PiR;
	}

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		incident = sample.CosineHemisphere;
		float pdf = CosineP(incident) * Scalars.PiR;

		if (FastMath.AlmostZero(CosineP(incident))) return Probable<RGB128>.Impossible;
		if (outgoing.Z > 0f) incident = Utility.NegateZ(incident);
		return (new RGB128(Scalars.PiR), pdf);
	}
}

/// <summary>
/// Perfectly uniform Lambertian diffuse scattering (both reflection and transmission).
/// </summary>
public sealed class Lambertian : BxDF
{
	public Lambertian() : base(FunctionType.Diffuse | FunctionType.Reflective | FunctionType.Transmissive) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => new(Scalars.TauR);

	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => FastMath.Abs(CosineP(incident)) * Scalars.TauR;

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		bool reflect = sample.x > 0.5f;
		sample = new Sample2D(FastMath.Abs(sample.x * 2f - 1f), sample.y);

		incident = sample.CosineHemisphere;
		float pdf = CosineP(incident) * Scalars.TauR;
		bool flip = (outgoing.Z > 0f) ^ reflect;

		if (flip) incident = Utility.NegateZ(incident);
		return (new RGB128(Scalars.TauR), pdf);
	}
}

/// <summary>
/// Microfacet diffuse reflection model originally proposed by
/// Generalization of Lambert's Reflectance Model [Oren and Nayar 1994].
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

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;

		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		float s = outgoing.Dot(incident) - cosO * cosI;
		if (FastMath.Positive(s)) s /= FastMath.Max(cosO, cosI);
		return new RGB128(a + b * s);
	}
}

/// <summary>
/// Regular <see cref="LambertianReflection"/> model with modification for <see cref="Materials.CoatedDiffuse"/>.
/// Simulates the interaction of light with a lambertian substrate travelling through a dielectric coating.
/// </summary>
public sealed class CoatedLambertianReflection : LambertianReflection
{
	public void Reset(in RGB128 newAlbedo, RealFresnel newFresnel)
	{
		float eta = newFresnel.etaAbove / newFresnel.etaBelow;
		float reflectance = FresnelDiffuseReflectanceFast(eta);
		Reset(newAlbedo, newFresnel, reflectance);
	}

	public void Reset(in RGB128 newAlbedo, RealFresnel newFresnel, float newReflectance)
	{
		fresnel = newFresnel;

		float eta = newFresnel.etaAbove / newFresnel.etaBelow;
		RGB128 denominator = RGB128.White - newAlbedo * newReflectance;
		multiplier = new RGB128(eta * eta * Scalars.PiR) / denominator;
	}

	RealFresnel fresnel;
	RGB128 multiplier;

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;

		//Only contains the diffuse component because specular is impossible to evaluate
		float evaluatedOutgoing = fresnel.Evaluate(FastMath.Abs(CosineP(outgoing)));
		float evaluatedIncident = fresnel.Evaluate(FastMath.Abs(CosineP(incident)));

		return multiplier * (1f - evaluatedOutgoing) * (1f - evaluatedIncident);
	}

	/// <summary>
	/// Calculates the exact fraction of light reflected back from the fresnel surface, given a hemisphere of diffuse directions. 
	/// </summary>
	/// <param name="eta">The quotient of the eta above and below the fresnel interface</param>
	/// <remarks>Equation based on The Reflection Factor of a Polished Glass Surface for Diffused Light [Walsh 1926].</remarks>
	public static float FresnelDiffuseReflectance(float eta)
	{
		if (eta.AlmostEquals(1f)) return 0f;
		if (eta >= 1f) return EntranceReflectance(eta);
		float reflectance = EntranceReflectance(1f / eta);
		return 1f - eta * eta * (1f - reflectance);

		static float EntranceReflectance(float eta)
		{
			float eta2 = eta * eta;
			float eta4 = eta2 * eta2;

			float eta1a1 = eta + 1f;
			float eta2a1 = eta2 + 1f;
			float eta4a1 = eta4 + 1f;

			float eta1s1 = eta - 1f;
			float eta2s1 = eta2 - 1f;
			float eta4s1 = eta4 - 1f;

			float quotient0 = eta1s1 * FastMath.FMA(3f, eta, 1f) / (6f * eta1a1 * eta1a1);
			float quotient1 = eta2 * eta2s1 * eta2s1 / (eta2a1 * eta2a1 * eta2a1);
			float quotient2 = -2f * eta2 * eta * (eta2 + eta + eta1s1) / (eta2a1 * eta4s1);
			float quotient3 = 8f * eta4 * eta4a1 / (eta2a1 * eta4s1 * eta4s1);

			float fma0 = FastMath.FMA(MathF.Log(eta1s1 / eta1a1), quotient1, quotient0);
			float fma1 = FastMath.FMA(MathF.Log(eta), quotient3, quotient2);

			return 0.5f + fma0 + fma1;
		}
	}

	/// <summary>
	/// Identical to <see cref="FresnelDiffuseReflectance"/> except using simpler equations to calculate an approximation.
	/// </summary>
	/// <remarks>Equation based on A Quantized-diffusion Model for Rendering Translucent Materials [D'Eon and Irving 2011].</remarks>
	public static float FresnelDiffuseReflectanceFast(float eta)
	{
		if (eta >= 1f) return EntranceReflectance(1f / eta);
		float reflectance = EntranceReflectance(eta);
		return 1f - eta * eta * (1f - reflectance);

		static float EntranceReflectance(float etaR)
		{
			const float Coefficient0 = +0.91932f;
			const float Coefficient1 = -3.47930f;
			const float Coefficient2 = +6.75335f;
			const float Coefficient3 = -7.80989f;
			const float Coefficient4 = +4.98554f;
			const float Coefficient5 = -1.36881f;

			float etaR2 = etaR * etaR;
			float etaR3 = etaR2 * etaR;
			float etaR4 = etaR2 * etaR2;
			float etaR5 = etaR4 * etaR;

			float sum = Coefficient0;

			sum = FastMath.FMA(Coefficient1, etaR, sum);
			sum = FastMath.FMA(Coefficient2, etaR2, sum);
			sum = FastMath.FMA(Coefficient3, etaR3, sum);
			sum = FastMath.FMA(Coefficient4, etaR4, sum);
			sum = FastMath.FMA(Coefficient5, etaR5, sum);

			return sum;
		}
	}

	/// <summary>
	/// Identical to <see cref="FresnelDiffuseReflectance"/> except calculated using Monte Carlo convergence.
	/// </summary>
	/// <remarks>Only used as a ground truth baseline for the previous two methods.
	/// Graph available here: https://www.desmos.com/calculator/g3nztbege6</remarks>
	public static float FresnelDiffuseReflectanceConverge(float eta, int sampleCount = (int)1E6)
	{
		var distribution = new StratifiedDistribution { Extend = sampleCount };
		var fresnel = new RealFresnel(eta, 1f);

		distribution.BeginSeries(Int2.Zero);
		Summation sum = Summation.Zero;

		for (int i = 0; i < distribution.Extend; i++)
		{
			distribution.BeginSession();

			Float3 outgoing = -distribution.Next2D().CosineHemisphere;
			float evaluated = fresnel.Evaluate(CosineP(outgoing));

			sum += (Float4)evaluated;
		}

		return sum.Result.X / distribution.Extend;
	}
}