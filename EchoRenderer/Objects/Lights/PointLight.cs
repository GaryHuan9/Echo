using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Sampling;

namespace EchoRenderer.Objects.Lights
{
	public class PointLight : Light
	{
		public override Float3 Power => 2f * Scalars.TAU * Intensity;

		public override Float3 Sample(in Float3 position, in Distro2 distro, out Float3 incident, out float pdf, out float distance)
		{
			pdf = 1f;

			Float3 difference = Position - position;

			distance = difference.Magnitude;
			incident = difference / distance;

			return Intensity / (distance * distance);
		}

		public override float ProbabilityDensity(in Float3 position, in Float3 incident) => 0f;
	}
}