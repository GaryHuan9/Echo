using System;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// A delta light that comes from a single direction, infinitely far away.
/// </summary>
/// <remarks>Light travels in parallel in the local <see cref="Float3.Forward"/> direction of this <see cref="DirectionalLight"/>.</remarks>
[EchoSourceUsable]
public sealed class DirectionalLight : InfiniteLight
{
	public DirectionalLight() => DirectlyVisible = false;

	Float3 incidentDirection;
	RGB128 scaledIntensity;
	float cosAngle;

	// float _angle = 0.6f;
	float _angle = 6f;
	float _power;

	//The angle at which directions can deviate, also half of the angular diameter
	[EchoSourceUsable]
	public float Angle
	{
		get => _angle;
		set
		{
			if (value is >= 0f and <= 90f) _angle = value;
			else throw new ArgumentOutOfRangeException(nameof(value));
		}
	}

	public override float Power => _power;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);

		float radians = Scalars.ToRadians(Angle);

		incidentDirection = -InverseTransform.MultiplyDirection(Float3.Forward);
		scaledIntensity = Intensity / (0.5f - 0.5f * MathF.Cos(radians * 2f));
		cosAngle = MathF.Cos(radians);

		//Approximates that the total energy to be half of the scene's bounding disk area
		const float Multiplier = Scalars.Pi * 0.5f;
		float radius = scene.accelerator.SphereBound.radius;
		_power = Intensity.Luminance * Multiplier * radius * radius;
	}

	public override bool IsDelta => false;

	public override RGB128 Evaluate(Float3 incident)
	{
		float cosIncident = incidentDirection.Dot(incident);
		return cosIncident >= cosAngle ? scaledIntensity : RGB128.Black;
	}

	public override float ProbabilityDensity(in GeometryPoint origin, Float3 incident)
	{
		return Sample2D.UniformSpherePdf;
	}

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		incident = sample.UniformSphere;
		travel = float.PositiveInfinity;

		return (Evaluate(incident), ProbabilityDensity(origin, incident));
	}

	// public override bool IsDelta => true;
	// public override RGB128 Evaluate(Float3 incident) => RGB128.Black;
	// public override float ProbabilityDensity(in GeometryPoint origin, Float3 incident) => 0f;
	//
	// public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	// {
	// 	incident = incidentDirection;
	// 	travel = float.PositiveInfinity;
	// 	return (Intensity, 1f);
	// }
}