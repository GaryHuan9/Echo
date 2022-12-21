using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public sealed class GlossyReflection<TMicrofacet, TFresnel> : BxDF where TMicrofacet : IMicrofacet
																   where TFresnel : IFresnel
{
	public GlossyReflection() : base(FunctionType.Glossy | FunctionType.Reflective) { }

	TMicrofacet microfacet;
	TFresnel fresnel;

	public void Reset(in TMicrofacet newMicrofacet, in TFresnel newFresnel)
	{
		microfacet = newMicrofacet;
		fresnel = newFresnel;
	}

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;
		if (!FindNormal(outgoing, incident, out Float3 normal)) return RGB128.Black;
		
		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(FastMath.Abs(outgoing.Dot(normal))) / (4f * cosO * cosI);
		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		return evaluated * ratio;
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;
		if (!FindNormal(outgoing, incident, out Float3 normal)) return 0f;

		return microfacet.ProbabilityDensity(outgoing, normal) / outgoing.Dot(normal) / 4f;
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		Float3 normal = microfacet.Sample(outgoing, sample);
		incident = Float3.Reflect(outgoing, normal);

		Ensure.AreEqual(normal.SquaredMagnitude, 1f);
		Ensure.AreEqual(incident.SquaredMagnitude, 1f);

		if (FlatOrOppositeHemisphere(outgoing, incident)) return Probable<RGB128>.Impossible;

		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(FastMath.Abs(outgoing.Dot(normal))) / (4f * cosO * cosI);
		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		float pdf = microfacet.ProbabilityDensity(outgoing, normal) / FastMath.Abs(outgoing.Dot(normal) * 4f);

		return (evaluated * ratio, pdf);
	}

	static bool FindNormal(in Float3 outgoing, in Float3 incident, out Float3 normal)
	{
		normal = outgoing + incident;
		float length2 = normal.SquaredMagnitude;

		if (!FastMath.Positive(length2)) return false;
		normal *= FastMath.SqrtR0(length2); //Normalize
		if (normal.Z < 0f) normal = -normal;

		return true;
	}
}

public sealed class GlossyTransmission<TMicrofacet> : BxDF where TMicrofacet : IMicrofacet
{
	public GlossyTransmission() : base(FunctionType.Glossy | FunctionType.Transmissive) { }

	TMicrofacet microfacet;
	RealFresnel fresnel;

	public void Reset(in TMicrofacet newMicrofacet, RealFresnel newFresnel)
	{
		microfacet = newMicrofacet;
		fresnel = newFresnel;
	}

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return RGB128.Black;

		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);
		
		fresnel.GetIndices(cosI, out float etaI, out float etaT);
		float eta = etaI / etaT;

		Float3 normal = FindNormal(outgoing, incident, eta);

		float dotO = outgoing.Dot(normal);
		float dotI = incident.Dot(normal);

		if (FastMath.Positive(dotO * dotI)) return RGB128.Black;
		RGB128 evaluated = RGB128.White - fresnel.Evaluate(dotO);

		float numerator = eta * eta * dotO * dotI;
		float denominator = FastMath.FMA(eta, dotI, dotO);
		denominator *= denominator * cosO * cosI;

		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		return evaluated * FastMath.Abs(ratio * numerator / denominator);
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return 0f;

		float eta = CosineP(outgoing) > 0f ? fresnel.etaBelow / fresnel.etaAbove : fresnel.etaAbove / fresnel.etaBelow;

		Float3 normal = FindNormal(outgoing, incident, eta);

		float dotO = outgoing.Dot(normal);
		float dotI = incident.Dot(normal);

		if (FastMath.Positive(dotO * dotI)) return 0f;
		float denominator = FastMath.FMA(eta, dotI, dotO);

		return microfacet.ProbabilityDensity(outgoing, normal) * FastMath.Abs(eta * eta * dotI) / (denominator * denominator);
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		if (FastMath.AlmostZero(outgoing.Z, 0f))
		{
			incident = default;
			return Probable<RGB128>.Impossible;
		}

		Float3 normal = microfacet.Sample(outgoing, sample);
		float dotO = outgoing.Dot(normal);

		if (FastMath.AlmostZero(dotO))
		{
			incident = default;
			return Probable<RGB128>.Impossible;
		}

		if (dotO < 0f) normal = -normal;

		float eta = CosineP(outgoing) > 0f ? fresnel.etaAbove / fresnel.etaBelow : fresnel.etaBelow / fresnel.etaAbove;

		if (!Refract(outgoing, normal, eta, out incident)) return Probable<RGB128>.Impossible;
		incident = incident.Normalized;

		return (Evaluate(outgoing, incident), ProbabilityDensity(outgoing, incident));
	}

	public static Float3 FindNormal(in Float3 incident, in Float3 transmit, float eta)
	{
		Float3 normal = (incident + transmit * eta).Normalized;
		return normal.Z < 0f ? -normal : normal;
	}

	public static bool Refract(in Float3 outgoing, in Float3 normal, float eta, out Float3 incident)
	{
		float cosThetaI = normal.Dot(outgoing);
		float sin2ThetaI = FastMath.Max0(1f - cosThetaI * cosThetaI);
		float sin2ThetaT = eta * eta * sin2ThetaI;

		if (sin2ThetaT >= 1)
		{
			incident = default;
			return false;
		}

		float cosThetaT = FastMath.Sqrt0(1f - sin2ThetaT);

		incident = eta * -outgoing + (eta * cosThetaI - cosThetaT) * normal;
		return true;
	}
}