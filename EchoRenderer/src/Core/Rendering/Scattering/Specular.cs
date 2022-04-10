using System;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

public class SpecularReflection : BxDF
{
	public SpecularReflection() : base(FunctionType.specular | FunctionType.reflective) { }

	public void Reset(in RGB128 newReflectance)
	{
		reflectance = newReflectance;
		mode = Mode.none;
	}

	public void Reset(in RGB128 newReflectance, in FresnelDielectric newDielectric)
	{
		reflectance = newReflectance;
		dielectric = newDielectric;
		mode = Mode.dielectric;
	}

	public void Reset(in RGB128 newReflectance, in FresnelConductor newConductor)
	{
		reflectance = newReflectance;
		conductor = newConductor;
		mode = Mode.conductor;
	}

	RGB128 reflectance;
	Mode mode;

	FresnelDielectric dielectric;
	FresnelConductor conductor;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(in Float3 outgoing, Sample2D sample, out Float3 incident)
	{
		incident = new Float3(-outgoing.X, -outgoing.Y, outgoing.Z);
		float cosI = CosineP(incident);

		RGB128 evaluated = mode switch
		{
			Mode.dielectric => dielectric.Evaluate(cosI),
			Mode.conductor => conductor.Evaluate(cosI),
			_ => RGB128.White
		};

		return (evaluated * reflectance / FastMath.Abs(cosI), 1f);
	}

	enum Mode
	{
		none,
		dielectric,
		conductor
	}
}

public class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.specular | FunctionType.transmissive) { }

	public void Reset(in RGB128 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		dielectric = new FresnelDielectric(newEtaAbove, newEtaBelow);
	}

	RGB128 transmittance;
	FresnelDielectric dielectric;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(in Float3 outgoing, Sample2D sample, out Float3 incident)
	{
		RGB128 evaluated = dielectric.Evaluate(outgoing, out incident);
		return (evaluated * transmittance / Math.Abs(CosineP(incident)), 1f);
	}
}

public class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.specular | FunctionType.reflective | FunctionType.transmissive) { }
	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => throw new NotImplementedException();
}