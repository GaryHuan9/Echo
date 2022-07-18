using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public class SpecularReflection : BxDF
{
	public SpecularReflection() : base(FunctionType.Specular | FunctionType.Reflective) { }

	public void Reset(in RGB128 newReflectance)
	{
		reflectance = newReflectance;
		mode = Mode.None;
	}

	public void Reset(in RGB128 newReflectance, in FresnelDielectric newDielectric)
	{
		reflectance = newReflectance;
		dielectric = newDielectric;
		mode = Mode.Dielectric;
	}

	public void Reset(in RGB128 newReflectance, in FresnelConductor newConductor)
	{
		reflectance = newReflectance;
		conductor = newConductor;
		mode = Mode.Conductor;
	}

	RGB128 reflectance;
	Mode mode;

	FresnelDielectric dielectric;
	FresnelConductor conductor;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		incident = new Float3(-outgoing.X, -outgoing.Y, outgoing.Z);
		float cosI = CosineP(incident);

		RGB128 evaluated = mode switch
		{
			Mode.Dielectric => dielectric.Evaluate(cosI),
			Mode.Conductor  => conductor.Evaluate(cosI),
			_               => RGB128.White
		};

		return (evaluated * reflectance / FastMath.Abs(cosI), 1f);
	}

	enum Mode
	{
		None,
		Dielectric,
		Conductor
	}
}

public class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.Specular | FunctionType.Transmissive) { }

	public void Reset(in RGB128 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		dielectric = new FresnelDielectric(newEtaAbove, newEtaBelow);
	}

	RGB128 transmittance;
	FresnelDielectric dielectric;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		RGB128 evaluated = RGB128.White - dielectric.Evaluate(outgoing, out incident);
		return (evaluated * transmittance / Math.Abs(CosineP(incident)), 1f);
	}
}

public class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.Specular | FunctionType.Reflective | FunctionType.Transmissive) { }
	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => throw new NotImplementedException();
}