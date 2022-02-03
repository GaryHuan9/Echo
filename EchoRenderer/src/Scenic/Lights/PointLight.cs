using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Scenic.Lights
{
	public class PointLight : LightSource
	{
		public override Float3 Power => 4f * Scalars.PI * Intensity;

		public override Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incident, out float pdf, out float travel)
		{
			Float3 difference = Position - interaction;

			travel = difference.Magnitude;
			float travelR = 1f / travel;

			if (FastMath.AlmostZero(travel))
			{
				pdf = 0f;
				incident = default;
				return Float3.zero;
			}

			pdf = 1f;

			incident = difference * travelR;
			return Intensity * travelR * travelR;
		}
	}
}