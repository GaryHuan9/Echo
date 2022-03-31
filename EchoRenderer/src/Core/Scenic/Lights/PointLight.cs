using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
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

		_power = 4f * Scalars.Pi * PackedMath.GetLuminance(Utilities.ToVector(Intensity));
	}

	public override Probable<RGBA128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel)
	{
		Float3 delta = Position - point;
		float travel2 = delta.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			incident = Float3.Zero;
			travel = 0f;
			return Probable<RGBA128>.Zero;
		}

		travel = FastMath.Sqrt0(travel2);

		float travelR = 1f / travel;
		incident = delta * travelR;

		return (Intensity * travelR * travelR, 1f);
	}
}