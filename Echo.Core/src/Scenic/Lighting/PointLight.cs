using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

public class PointLight : LightSource, ILightSource<PreparedPointLight>
{
	float _power;

	public override float Power => _power;

	public override void Prepare(PreparedSceneOld scene)
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

	public PreparedPointLight Extract() => new(Intensity, ContainedPosition);
}

public readonly struct PreparedPointLight
{
	public PreparedPointLight(RGB128 intensity, Float3 position)
	{
		this.intensity = intensity;
		this.position = position;
		energy = 2f * Scalars.Tau * intensity.Luminance;
	}

	readonly RGB128 intensity;
	readonly Float3 position;
	readonly float energy;

	public AxisAlignedBoundingBox BoxBounds => new(position, position);

	public ConeBounds ConeBounds => ConeBounds.CreateFullSphere();

	public LightBounds LightBounds => new(BoxBounds, ConeBounds, energy);

	[SkipLocalsInit]
	public Probable<RGB128> Sample(in GeometryPoint point, out Float3 incident, out float travel)
	{
		Float3 offset = position - point;
		float travel2 = offset.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			Unsafe.SkipInit(out incident);
			Unsafe.SkipInit(out travel);
			return Probable<RGB128>.Impossible;
		}

		travel = FastMath.Sqrt0(travel2);

		float travelR = 1f / travel;
		incident = offset * travelR;

		return new Probable<RGB128>(intensity * travelR * travelR, 1f);
	}
}