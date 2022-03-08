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

	public override Float3 Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float pdf, out float travel)
	{
		Float3 delta = Position - point;
		float travel2 = delta.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			incident = default;
			pdf = travel = default;
			return Float3.zero;
		}

		pdf = 1f;
		travel = FastMath.Sqrt0(travel2);

		float travelR = 1f / travel;
		incident = delta * travelR;

		return Intensity * travelR * travelR;
	}
}
