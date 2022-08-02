using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public class GlossyReflection<TMicrofacet, TFresnel> : BxDF where TMicrofacet : IMicrofacet
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
		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		if (FastMath.AlmostZero(cosO) || FastMath.AlmostZero(cosI)) return RGB128.Black;

		Float3 normal = outgoing + incident;
		float length2 = normal.SquaredMagnitude;

		if (FastMath.AlmostZero(length2)) return RGB128.Black;
		normal *= FastMath.SqrtR0(length2); //Normalize

		float fraction = microfacet.ProjectedArea(normal) * microfacet.Visibility(outgoing, incident);
		return fresnel.Evaluate(outgoing.Dot(normal)) * fraction / (4f * cosO * cosI);
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident)
	{
		if (!SameHemisphere(outgoing, incident)) return 0f;

		Float3 normal = outgoing + incident;
		float length2 = normal.SquaredMagnitude;

		if (FastMath.AlmostZero(length2)) return 0f;
		normal *= FastMath.SqrtR0(length2); //Normalize

		return microfacet.ProbabilityDensity(outgoing, normal) / outgoing.Dot(normal) / 4f;
	}

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		Float3 normal = microfacet.Sample(outgoing, sample);
		incident = Float3.Reflect(outgoing, normal);

		if (!SameHemisphere(outgoing, incident)) return Probable<RGB128>.Impossible;
		return (Evaluate(outgoing, incident), ProbabilityDensity(outgoing, incident));
	}
}

public class GlossyTransmission<TMicrofacet> : BxDF where TMicrofacet : IMicrofacet
{
	public GlossyTransmission() : base(FunctionType.Glossy | FunctionType.Transmissive) { }

	TMicrofacet microfacet;
	RealFresnel fresnel;

	public void Reset(in TMicrofacet newMicrofacet, float newEtaAbove, float newEtaBelow)
	{
		microfacet = newMicrofacet;
		fresnel = new RealFresnel(newEtaAbove, newEtaBelow);
	}

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident)
	{
		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		if (FastMath.AlmostZero(cosO) || FastMath.AlmostZero(cosI)) return RGB128.Black;

		throw new NotImplementedException();
	}
}