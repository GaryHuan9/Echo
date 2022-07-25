using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public class SpecularReflection<TFresnel> : BxDF where TFresnel : IFresnel
{
	public SpecularReflection() : base(FunctionType.Specular | FunctionType.Reflective) { }

	public void Reset(in RGB128 newReflectance, in TFresnel newFresnel)
	{
		reflectance = newReflectance;
		fresnel = newFresnel;
	}

	RGB128 reflectance;
	TFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		incident = Reflect(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(cosI);
		return (evaluated * reflectance / FastMath.Abs(cosI), 1f);
	}

	public static Float3 Reflect(in Float3 outgoing) => new(-outgoing.X, -outgoing.Y, outgoing.Z);
}

public class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.Specular | FunctionType.Transmissive) { }

	public void Reset(in RGB128 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		fresnel = new DielectricFresnel(newEtaAbove, newEtaBelow);
	}

	RGB128 transmittance;
	DielectricFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		float cosI = CosineP(outgoing);
		float evaluated = fresnel.Evaluate(ref cosI, out float cosT, out float eta);

		evaluated = 1f - evaluated;

		if (FastMath.AlmostZero(evaluated))
		{
			incident = Normal(outgoing);
			return (RGB128.Black, 1f);
		}

		incident = Transmit(outgoing, cosI, cosT, eta);
		return (transmittance * evaluated / FastMath.Abs(CosineP(incident)), 1f);
	}

	public static Float3 Transmit(in Float3 outgoing, float cosI, float cosT, float eta)
	{
		Float3 incident = Normal(outgoing);
		incident *= eta * cosI - cosT;
		incident -= eta * outgoing;
		return incident;
	}
}

public class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.Specular | FunctionType.Reflective | FunctionType.Transmissive) { }

	public void Reset(in RGB128 newScatter, float newEtaAbove, float newEtaBelow)
	{
		scatter = newScatter;
		fresnel = new DielectricFresnel(newEtaAbove, newEtaBelow);
	}

	RGB128 scatter;
	DielectricFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		float cosI = CosineP(outgoing);
		float evaluated = fresnel.Evaluate(ref cosI, out float cosT, out float eta);

		if (sample.x < evaluated)
		{
			//Perform specular reflection
			incident = SpecularReflection<DielectricFresnel>.Reflect(outgoing);
			return (scatter * evaluated / FastMath.Abs(CosineP(incident)), evaluated);
		}

		//Perform specular transmission
		evaluated = 1f - evaluated;
		incident = SpecularTransmission.Transmit(outgoing, cosI, cosT, eta);
		return (scatter * evaluated / FastMath.Abs(CosineP(incident)), evaluated);
	}
}