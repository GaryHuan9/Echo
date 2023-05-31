using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// A delta light that comes from a single direction, infinitely far away.
/// </summary>
/// <remarks>Light travels in parallel in the local <see cref="Float3.Forward"/> direction of this <see cref="DirectionalLight"/>.</remarks>
public class DirectionalLight : InfiniteLight
{
	Float3 incidentDirection;
	float _power;

	public override float Power => _power;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);

		incidentDirection = -InverseTransform.MultiplyDirection(Float3.Forward);

		//Approximates that the total energy to be half of the scene's bounding disk area
		const float Multiplier = Scalars.Pi * 0.5f;
		float radius = scene.accelerator.SphereBound.radius;
		_power = Intensity.Luminance * Multiplier * radius * radius;
	}

	public override RGB128 Evaluate(in Float3 direction) => RGB128.Black;
	public override float ProbabilityDensity(in GeometryPoint origin, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		incident = incidentDirection;
		travel = float.PositiveInfinity;
		return (Intensity, 1f);
	}
}