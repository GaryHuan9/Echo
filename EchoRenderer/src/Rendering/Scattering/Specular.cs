using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Rendering.Scattering;

public class SpecularReflection : BxDF
{
	public SpecularReflection() : base
	(
		FunctionType.reflective |
		FunctionType.specular
	) { }

	public void Reset(in Float3 newReflectance, in FresnelDielectric newDielectric)
	{
		reflectance = newReflectance;
		dielectric = newDielectric;
		isDielectric = true;
	}

	public void Reset(in Float3 newReflectance, in FresnelConductor newConductor)
	{
		reflectance = newReflectance;
		conductor = newConductor;
		isDielectric = false;
	}

	Float3 reflectance;
	bool isDielectric;

	FresnelDielectric dielectric;
	FresnelConductor conductor;

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => Float3.zero;

	public override Float3 Sample(in Float3 outgoing, Distro2 distro, out Float3 incident, out float pdf)
	{
		incident = new Float3(-outgoing.x, -outgoing.y, outgoing.z);

		pdf = 1f;
		float cosI = CosineP(incident);

		Float3 evaluation = isDielectric ? dielectric.Evaluate(cosI) : conductor.Evaluate(cosI);
		return evaluation * reflectance / AbsoluteCosineP(incident);
	}

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;
}

public class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base
	(
		FunctionType.transmissive |
		FunctionType.specular
	) { }

	public void Reset(in Float3 newTransmittance, float newEtaAbove, float newEtaBelow)
	{
		transmittance = newTransmittance;
		etaAbove = newEtaAbove;
		etaBelow = newEtaBelow;

		fresnel = new FresnelDielectric(etaAbove, etaBelow);
	}

	Float3 transmittance;
	float etaAbove;
	float etaBelow;

	FresnelDielectric fresnel;

	//TODO

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => Float3.zero;

	public override Float3 Sample(in Float3 outgoing, Distro2 distro, out Float3 incident, out float pdf) => throw new NotImplementedException();

	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;
}