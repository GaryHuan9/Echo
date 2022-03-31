using System;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

public class SpecularReflection : BxDF
{
	public SpecularReflection() : base(FunctionType.specular | FunctionType.reflective) { }

	public void Reset(in RGBA32 newReflectance)
	{
		reflectance = newReflectance;
		mode = Mode.none;
	}

	public void Reset(in RGBA32 newReflectance, in FresnelDielectric newDielectric)
	{
		reflectance = newReflectance;
		dielectric = newDielectric;
		mode = Mode.dielectric;
	}

	public void Reset(in RGBA32 newReflectance, in FresnelConductor newConductor)
	{
		reflectance = newReflectance;
		conductor = newConductor;
		mode = Mode.conductor;
	}

	RGBA32 reflectance;
	Mode mode;

	FresnelDielectric dielectric;
	FresnelConductor conductor;

	public override RGBA32 Evaluate(in Float3 outgoing, in Float3 incident) => RGBA32.Zero;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGBA32> Sample(in Float3 outgoing, Sample2D sample, out Float3 incident)
	{
		incident = new Float3(-outgoing.X, -outgoing.Y, outgoing.Z);
		float cosI = CosineP(incident);

		RGBA32 evaluated = mode switch
		{
			Mode.dielectric => dielectric.Evaluate(cosI),
			Mode.conductor => conductor.Evaluate(cosI),
			_ => RGBA32.White
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

	public void Reset(in RGBA32 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		dielectric = new FresnelDielectric(newEtaAbove, newEtaBelow);
	}

	RGBA32 transmittance;
	FresnelDielectric dielectric;

	public override RGBA32 Evaluate(in Float3 outgoing, in Float3 incident) => RGBA32.Zero;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGBA32> Sample(in Float3 outgoing, Sample2D sample, out Float3 incident)
	{
		RGBA32 evaluated = RGBA32.White - dielectric.Evaluate(outgoing, out incident);
		return (evaluated * transmittance / Math.Abs(CosineP(incident)), 1f);
	}
}

public class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.specular | FunctionType.reflective | FunctionType.transmissive) { }
	public override RGBA32 Evaluate(in Float3 outgoing, in Float3 incident) => throw new NotImplementedException();
}