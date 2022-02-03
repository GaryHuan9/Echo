using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Scenic.Lights
{
	public class GeometryLight : IAreaLight
	{
		public void Reset(in GeometryToken newToken)
		{
			token = newToken;
		}

		GeometryToken token;

		public Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incident, out float pdf, out float travel) => throw new NotImplementedException();

		public float ProbabilityDensity(in Interaction interaction, in Float3 incident) => throw new NotImplementedException();
	}
}