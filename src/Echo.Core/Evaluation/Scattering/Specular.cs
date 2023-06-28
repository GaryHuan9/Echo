using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public sealed class SpecularReflection<TFresnel> : BxDF where TFresnel : IFresnel
{
	public SpecularReflection() : base(FunctionType.Specular | FunctionType.Reflective) { }

	public void Reset(in TFresnel newFresnel) => fresnel = newFresnel;

	TFresnel fresnel;

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		incident = Reflect(outgoing);
		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(cosO);
		return (evaluated / FastMath.Abs(cosI), 1f);
	}

	public static Float3 Reflect(Float3 outgoing) => new(-outgoing.X, -outgoing.Y, outgoing.Z);
}

public sealed class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.Specular | FunctionType.Transmissive) { }

	public void Reset(RealFresnel newFresnel) => fresnel = newFresnel;

	RealFresnel fresnel;

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		var packet = fresnel.CreateIncomplete(CosineP(outgoing)).Complete;

		if (packet.TotalInternalReflection)
		{
			incident = default;
			return Probable<RGB128>.Impossible;
		}

		float evaluated = 1f - packet.Value;
		incident = packet.Refract(outgoing, Float3.Forward);
		evaluated /= FastMath.Abs(CosineP(incident));

		return (new RGB128(evaluated), 1f);
	}
}

public sealed class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.Specular | FunctionType.Reflective | FunctionType.Transmissive) { }

	public void Reset(RealFresnel newFresnel) => fresnel = newFresnel;

	RealFresnel fresnel;

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		var packet = fresnel.CreateIncomplete(CosineP(outgoing)).Complete;
		float evaluated = packet.Value;

		if (sample.x < evaluated)
		{
			//Specular reflection
			incident = SpecularReflection<RealFresnel>.Reflect(outgoing);
		}
		else
		{
			//Specular transmission
			evaluated = 1f - evaluated;
			incident = packet.Refract(outgoing, Float3.Forward);
		}

		return (new RGB128(evaluated) / FastMath.Abs(CosineP(incident)), evaluated);
	}
}

public sealed class SpecularLambertian : BxDF
{
	public SpecularLambertian() : base(FunctionType.Specular | FunctionType.Reflective) { }

	public void Reset(RGB128 newAlbedo, RealFresnel newFresnel)
	{
		float eta = newFresnel.etaAbove / newFresnel.etaBelow;
		float reflectance = FresnelDiffuseReflectanceFast(eta);
		Reset(newAlbedo, newFresnel, reflectance);
	}

	public void Reset(RGB128 newAlbedo, RealFresnel newFresnel, float newReflectance)
	{
		fresnel = newFresnel;

		float eta = newFresnel.etaAbove / newFresnel.etaBelow;
		RGB128 denominator = RGB128.White - newAlbedo * newReflectance;
		multiplier = new RGB128(eta * eta * Scalars.PiR) / denominator;
	}

	RealFresnel fresnel;
	RGB128 multiplier;

	//The current BSDF system does not support one BxDF with mixed specular and non specular samples
	//For now we treat this BxDF as a specular one for correct sampling at the cost of disabling MIS

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => 0f;

	// public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	// {
	// 	if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;
	//
	// 	//Only contains the diffuse component because specular is impossible to evaluate
	// 	float evaluatedOutgoing = fresnel.Evaluate(FastMath.Abs(CosineP(outgoing)));
	// 	float evaluatedIncident = fresnel.Evaluate(FastMath.Abs(CosineP(incident)));
	//
	// 	return multiplier * (1f - evaluatedOutgoing) * (1f - evaluatedIncident);
	// }
	//
	// public override float ProbabilityDensity(Float3 outgoing, Float3 incident)
	// {
	// 	if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;
	//
	// 	//Only contains the probability of the diffuse component
	// 	float evaluated = fresnel.Evaluate(FastMath.Abs(CosineP(outgoing)));
	// 	return (1f - evaluated) * FastMath.Abs(CosineP(incident)) * Scalars.PiR;
	// }

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		float cosO = FastMath.Abs(CosineP(outgoing));
		float evaluatedOutgoing = fresnel.Evaluate(cosO);

		if (sample.x < evaluatedOutgoing)
		{
			//Specular reflection
			incident = SpecularReflection<RealFresnel>.Reflect(outgoing);
			float cosI = FastMath.Abs(CosineP(incident));

			RGB128 evaluated = new RGB128(evaluatedOutgoing);
			return (evaluated / cosI, evaluatedOutgoing);
		}
		else
		{
			//Diffuse reflection with internal bounces
			sample = new Sample2D(sample.x.Stretch(evaluatedOutgoing, 1f), sample.y);

			incident = sample.CosineHemisphere;
			float cosI = CosineP(incident);

			if (outgoing.Z < 0f) incident = new Float3(incident.X, incident.Y, -incident.Z);

			float evaluatedIncident = 1f - fresnel.Evaluate(cosI);
			evaluatedOutgoing = 1f - evaluatedOutgoing;

			float evaluated = evaluatedOutgoing * evaluatedIncident;
			float pdf = evaluatedOutgoing * Scalars.PiR * cosI;
			return (multiplier * evaluated, pdf);
		}
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

			float eta1p1 = eta + 1f;
			float eta2p1 = eta2 + 1f;
			float eta4p1 = eta4 + 1f;

			float eta1m1 = eta - 1f;
			float eta2m1 = eta2 - 1f;
			float eta4m1 = eta4 - 1f;

			float quotient0 = eta1m1 * FastMath.FMA(3f, eta, 1f) / (6f * eta1p1 * eta1p1);
			float quotient1 = eta2 * eta2m1 * eta2m1 / (eta2p1 * eta2p1 * eta2p1);
			float quotient2 = -2f * eta2 * eta * (eta2 + eta + eta1m1) / (eta2p1 * eta4m1);
			float quotient3 = 8f * eta4 * eta4p1 / (eta2p1 * eta4m1 * eta4m1);

			float fma0 = FastMath.FMA(MathF.Log(eta1m1 / eta1p1), quotient1, quotient0);
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