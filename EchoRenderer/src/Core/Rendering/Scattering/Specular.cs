using System;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

public class SpecularReflection : BxDF
{
	public SpecularReflection() : base(FunctionType.specular | FunctionType.reflective) { }

	public void Reset(in Float3 newReflectance)
	{
		reflectance = newReflectance;
		mode = Mode.none;
	}

	public void Reset(in Float3 newReflectance, in FresnelDielectric newDielectric)
	{
		reflectance = newReflectance;
		dielectric = newDielectric;
		mode = Mode.dielectric;
	}

	public void Reset(in Float3 newReflectance, in FresnelConductor newConductor)
	{
		reflectance = newReflectance;
		conductor = newConductor;
		mode = Mode.conductor;
	}

	Float3 reflectance;
	Mode mode;

	FresnelDielectric dielectric;
	FresnelConductor conductor;

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => Float3.Zero;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Float3 Sample(in Float3 outgoing, Sample2D sample, out Float3 incident, out float pdf)
	{
		pdf = 1f;

		incident = new Float3(-outgoing.X, -outgoing.Y, outgoing.Z);
		float cosI = CosineP(incident);

		Float3 evaluated = mode switch
		{
			Mode.dielectric => dielectric.Evaluate(cosI),
			Mode.conductor => conductor.Evaluate(cosI),
			_ => Float3.One
		};

		return evaluated * reflectance / FastMath.Abs(cosI);
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

	public void Reset(in Float3 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		dielectric = new FresnelDielectric(newEtaAbove, newEtaBelow);
	}

	Float3 transmittance;
	FresnelDielectric dielectric;

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => Float3.Zero;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Float3 Sample(in Float3 outgoing, Sample2D sample, out Float3 incident, out float pdf)
	{
		pdf = 1f;

		Float3 evaluated = Float3.One - dielectric.Evaluate(outgoing, out incident);
		return evaluated * transmittance / Math.Abs(CosineP(incident));
	}
}

public class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.specular | FunctionType.reflective | FunctionType.transmissive) { }
	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => throw new NotImplementedException();
}