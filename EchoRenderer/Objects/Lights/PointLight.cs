using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Objects.Lights
{
	public class PointLight : Light
	{
		public override Float3 Power => 2f * Scalars.TAU * Intensity;

		public override bool IsDelta => true;

		public override Float3 Sample(in Interaction interaction, in Distro2 distro, out Float3 incidentWorld, out float pdf, out float travel)
		{
			pdf = 1f;

			Float3 difference = Position - interaction.position;

			travel = difference.Magnitude;

			incidentWorld = difference / travel;
			return Intensity / (travel * travel);
		}

		public override float ProbabilityDensity(in Interaction interaction, in Float3 incident) => 0f;
	}
}