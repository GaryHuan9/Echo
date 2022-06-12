using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Common.Mathematics.Primitives;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

public class PointLight : LightSource
{
	float _power;

	public override float Power => _power;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);

		_power = 4f * Scalars.Pi * Intensity.Luminance;
	}

	public override Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel)
	{
		Float3 delta = Position - point;
		float travel2 = delta.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			incident = Float3.Zero;
			travel = 0f;
			return Probable<RGB128>.Impossible;
		}

		travel = FastMath.Sqrt0(travel2);

		float travelR = 1f / travel;
		incident = delta * travelR;

		return (Intensity * travelR * travelR, 1f);
	}
}