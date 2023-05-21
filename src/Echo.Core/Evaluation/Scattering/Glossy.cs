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

	public void Reset(in TMicrofacet newMicrofacet, in TFresnel newFresnel)
	{
		microfacet = newMicrofacet;
		fresnel = newFresnel;
	}

	TMicrofacet microfacet;
	TFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;
		Float3 normal = FindNormal(outgoing, incident);

		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident) * 0.25f;
		RGB128 evaluated = fresnel.Evaluate(outgoing.Dot(normal)) / (CosineP(outgoing) * CosineP(incident));
		return evaluated * ratio;
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;
		Float3 normal = FindNormal(outgoing, incident);

		return microfacet.ProbabilityDensity(outgoing, normal) / FastMath.Abs(outgoing.Dot(normal) * 4f);
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		Float3 normal = microfacet.Sample(outgoing, sample);
		incident = Float3.Reflect(outgoing, normal);

		Ensure.AreEqual(normal.SquaredMagnitude, 1f);
		Ensure.AreEqual(incident.SquaredMagnitude, 1f);

		if (FlatOrOppositeHemisphere(outgoing, incident)) return Probable<RGB128>.Impossible;

		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident) * 0.25f;
		RGB128 evaluated = fresnel.Evaluate(outgoing.Dot(normal)) / (CosineP(outgoing) * CosineP(incident));
		float pdf = microfacet.ProbabilityDensity(outgoing, normal) / FastMath.Abs(outgoing.Dot(normal) * 4f);

		return (evaluated * ratio, pdf);
	}

	public static Float3 FindNormal(in Float3 outgoing, in Float3 incident)
	{
		Float3 normal = outgoing + incident;
		float length2 = normal.SquaredMagnitude;

		if (!FastMath.Positive(length2)) return Float3.Forward;

		normal *= FastMath.SqrtR0(length2); //Normalize
		return normal.Z < 0f ? -normal : normal;
	}
}

public sealed class GlossyTransmission<TMicrofacet> : BxDF where TMicrofacet : IMicrofacet
{
	public GlossyTransmission() : base(FunctionType.Glossy | FunctionType.Transmissive) { }

	public void Reset(in TMicrofacet newMicrofacet, RealFresnel newFresnel)
	{
		microfacet = newMicrofacet;
		fresnel = newFresnel;
	}

	TMicrofacet microfacet;
	RealFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return RGB128.Black;

		var packet = fresnel.CreateIncomplete(CosineP(outgoing));
		float etaR = packet.etaIncident / packet.etaOutgoing;
		Float3 normal = FindNormal(outgoing, incident, etaR);

		float dotO = outgoing.Dot(normal);
		float dotI = incident.Dot(normal);

		if (FastMath.Positive(dotO * dotI)) return RGB128.Black;
		RGB128 evaluated = RGB128.White - fresnel.Evaluate(dotO);
		if (evaluated.IsZero) return RGB128.Black;

		float numerator = etaR * etaR * dotO * dotI;
		float denominator = FastMath.FMA(etaR, dotI, dotO);
		denominator *= denominator;

		if (!FastMath.Positive(denominator)) denominator = 1f;
		denominator *= CosineP(outgoing) * CosineP(incident);

		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		return evaluated * ratio * FastMath.Abs(numerator / denominator);
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (FlatOrSameHemisphere(outgoing, incident)) return 0f;

		var packet = fresnel.CreateIncomplete(CosineP(outgoing));
		float etaR = packet.etaIncident / packet.etaOutgoing;
		Float3 normal = FindNormal(outgoing, incident, etaR);

		float dotO = outgoing.Dot(normal);
		float dotI = incident.Dot(normal);

		if (FastMath.Positive(dotO * dotI)) return 0f;
		float numerator = FastMath.Abs(etaR * etaR * dotI);
		float denominator = FastMath.FMA(etaR, dotI, dotO);
		denominator *= denominator;

		if (!FastMath.Positive(denominator)) denominator = 1f;
		return microfacet.ProbabilityDensity(outgoing, normal) * (numerator / denominator);
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		Float3 normal = microfacet.Sample(outgoing, sample);
		float dotO = outgoing.Dot(normal);

		var packet = fresnel.CreateIncomplete(dotO).Complete;

		if (packet.TotalInternalReflection)
		{
			incident = default;
			return Probable<RGB128>.Impossible;
		}

		incident = packet.Refract(outgoing, normal);
		float dotI = incident.Dot(normal);

		if (FlatOrSameHemisphere(outgoing, incident) || FastMath.Positive(dotO * dotI)) return Probable<RGB128>.Impossible;

		float etaR = packet.etaIncident / packet.etaOutgoing;
		float numerator = FastMath.Abs(etaR * etaR * dotI);
		float denominator = FastMath.FMA(etaR, dotI, dotO);
		denominator *= denominator;

		if (!FastMath.Positive(denominator)) denominator = 1f;

		float ratio = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		float evaluated = numerator * dotO / (denominator * CosineP(outgoing) * CosineP(incident));
		float pdf = microfacet.ProbabilityDensity(outgoing, normal) * (numerator / denominator);

		return (new RGB128(1f - packet.Value) * FastMath.Abs(evaluated) * ratio, pdf);
	}

	static Float3 FindNormal(in Float3 outgoing, in Float3 incident, float etaR) => GlossyReflection<TMicrofacet, RealFresnel>.FindNormal(outgoing, incident * etaR);
}