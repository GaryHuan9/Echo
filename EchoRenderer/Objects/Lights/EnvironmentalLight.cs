using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Objects.Lights
{
	public class EnvironmentalLight : LightSource
	{
		public EnvironmentalLight() : base(LightType.area | LightType.infinite) { }

		public override Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incidentWorld, out float pdf, out float travel) => throw new System.NotImplementedException();

		public override float ProbabilityDensity(in Interaction interaction, in Float3 incidentWorld) => throw new System.NotImplementedException();
	}
}