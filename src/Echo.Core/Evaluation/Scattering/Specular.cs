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

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		incident = Reflect(outgoing);
		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(cosO);
		return (evaluated / FastMath.Abs(cosI), 1f);
	}

	public static Float3 Reflect(in Float3 outgoing) => new(-outgoing.X, -outgoing.Y, outgoing.Z);
}

public sealed class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.Specular | FunctionType.Transmissive) { }

	public void Reset(RealFresnel newFresnel) => fresnel = newFresnel;

	RealFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
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

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
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
	public SpecularLambertian() : base(FunctionType.Diffuse | FunctionType.Reflective) { }

	public void Reset(in RGB128 newAlbedo, RealFresnel newFresnel)
	{
		float etaR = newFresnel.etaBelow / newFresnel.etaAbove;
		float reflectance = FresnelDiffuseReflectanceFast(etaR);
		Reset(newAlbedo, newFresnel, reflectance);
	}

	public void Reset(in RGB128 newAlbedo, RealFresnel newFresnel, float newReflectance)
	{
		fresnel = newFresnel;

		float eta = newFresnel.etaAbove / newFresnel.etaBelow;
		float eta2 = eta * eta;

		float reflectance = 1f - eta2 * (1f - newReflectance);
		RGB128 denominator = RGB128.White - newAlbedo * reflectance;
		multiplier = new RGB128(eta2 * Scalars.PiR) / denominator;
	}

	RealFresnel fresnel;
	RGB128 multiplier;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;

		//Only contains the diffuse component because specular is impossible to evaluate
		float evaluatedOutgoing = fresnel.Evaluate(FastMath.Abs(CosineP(outgoing)));
		float evaluatedIncident = fresnel.Evaluate(FastMath.Abs(CosineP(incident)));

		float evaluated = (1f - evaluatedOutgoing) * (1f - evaluatedIncident);
		return multiplier * evaluated;
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;

		//Only contains the probability of the diffuse component
		float evaluated = fresnel.Evaluate(FastMath.Abs(CosineP(outgoing)));
		return (1f - evaluated) * FastMath.Abs(CosineP(incident)) * Scalars.PiR;
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
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

			float evaluatedIncident = fresnel.Evaluate(cosI);
			float evaluated = (1f - evaluatedOutgoing) * (1f - evaluatedIncident);
			return (multiplier * evaluated, 1f - evaluatedOutgoing);
		}
	}

	public static float FresnelDiffuseReflectance(float etaR)
	{
		float etaR2 = etaR * etaR;
		float etaR4 = etaR2 * etaR2;

		float etaR1p1 = etaR + 1f;
		float etaR2p1 = etaR2 + 1f;
		float etaR4p1 = etaR4 + 1f;

		float etaR1m1 = etaR - 1f;
		float etaR2m1 = etaR2 - 1f;
		float etaR4m1 = etaR4 - 1f;

		float quotient0 = etaR1m1 * FastMath.FMA(3f, etaR, 1f) / (6f * etaR1p1 * etaR1p1);
		float quotient1 = etaR2 * etaR2m1 * etaR2m1 / (etaR2p1 * etaR2p1 * etaR2p1);
		float quotient2 = -2f * etaR2 * etaR * (etaR2 + etaR + etaR1m1) / (etaR2p1 * etaR4m1);
		float quotient3 = 8f * etaR4 * etaR4p1 / (etaR2p1 * etaR4m1 * etaR4m1);

		float fma0 = FastMath.FMA(MathF.Log(etaR1m1 / etaR1p1), quotient1, quotient0);
		float fma1 = FastMath.FMA(MathF.Log(etaR), quotient3, quotient2);

		return 0.5f + fma0 + fma1;
	}

	static float FresnelDiffuseReflectanceFast(float eta)
	{
		const float Coefficient0 = +0.91932f;
		const float Coefficient1 = -3.47930f;
		const float Coefficient2 = +6.75335f;
		const float Coefficient3 = -7.80989f;
		const float Coefficient4 = +4.98554f;
		const float Coefficient5 = -1.36881f;

		float eta1 = 1f / eta;
		float eta2 = eta1 * eta1;
		float eta3 = eta2 * eta1;
		float eta4 = eta2 * eta2;
		float eta5 = eta4 * eta1;

		float sum = Coefficient0;

		sum = FastMath.FMA(Coefficient1, eta1, sum);
		sum = FastMath.FMA(Coefficient2, eta2, sum);
		sum = FastMath.FMA(Coefficient3, eta3, sum);
		sum = FastMath.FMA(Coefficient4, eta4, sum);
		sum = FastMath.FMA(Coefficient5, eta5, sum);

		return sum;
	}
}