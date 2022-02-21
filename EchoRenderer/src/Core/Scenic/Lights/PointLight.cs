using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Scenic.Lights;

public class PointLight : LightSource
{
	float _power;

	public override float Power => _power;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);

		_power = 4f * Scalars.PI * PackedMath.GetLuminance(Utilities.ToVector(Intensity));
	}

	public override Float3 Sample(in GeometryPoint point, Distro2 distro, out Float3 incident, out float pdf, out float travel)
	{
		Float3 offset = Position - point;

		travel = offset.Magnitude;
		float travelR = 1f / travel;

		if (FastMath.AlmostZero(travel))
		{
			pdf = 0f;
			incident = default;
			return Float3.zero;
		}

		pdf = 1f;

		incident = offset * travelR;
		return Intensity * travelR * travelR;
	}
}