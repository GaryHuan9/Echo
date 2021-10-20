using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	public class SpecularReflection : BidirectionalDistributionFunction
	{
		public SpecularReflection() : base
		(
			BidirectionalDistributionFunctionType.reflection |
			BidirectionalDistributionFunctionType.specular
		) { }

		public void Reset(in Float3 newReflectance, in FresnelDielectric newDielectric)
		{
			reflectance  = newReflectance;
			dielectric   = newDielectric;
			isDielectric = true;
		}

		public void Reset(in Float3 newReflectance, in FresnelConductor newConductor)
		{
			reflectance  = newReflectance;
			conductor    = newConductor;
			isDielectric = false;
		}

		Float3 reflectance;
		bool   isDielectric;

		FresnelDielectric dielectric;
		FresnelConductor  conductor;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => Float3.zero;

		public override Float3 Sample(in Float3 outgoing, out Float3 incident, in Float2 sample, out float pdf, ref BidirectionalDistributionFunctionType type)
		{
			incident = new Float3(-outgoing.x, -outgoing.y, outgoing.z);

			pdf = 1f;
			float cosI = Cosine(incident);

			Float3 evaluation = isDielectric ? dielectric.Evaluate(cosI) : conductor.Evaluate(cosI);
			return evaluation * reflectance / AbsoluteCosine(incident);
		}
	}

	public class SpecularTransmission : BidirectionalDistributionFunction
	{
		public SpecularTransmission() : base
		(
			BidirectionalDistributionFunctionType.transmission |
			BidirectionalDistributionFunctionType.specular
		) { }

		public void Reset(in Float3 newTransmittance, float newEtaAbove, float newEtaBelow)
		{
			transmittance = newTransmittance;
			etaAbove      = newEtaAbove;
			etaBelow      = newEtaBelow;

			fresnel = new FresnelDielectric(etaAbove, etaBelow);
		}

		Float3 transmittance;
		float  etaAbove;
		float  etaBelow;

		FresnelDielectric fresnel;

		//TODO

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => Float3.zero;

		public override Float3 Sample(in Float3 outgoing, out Float3 incident, in Float2 sample, out float pdf, ref BidirectionalDistributionFunctionType type) => throw new NotImplementedException();
	}
}