using System;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// An <see cref="InfiniteLight"/> that emits from a single general direction, infinitely far away.
/// </summary>
/// <remarks>Light travels in parallel in the local <see cref="Float3.Forward"/> direction
/// of this <see cref="DirectionalLight"/>. This is a convenient way of creating a sun.</remarks>
[EchoSourceUsable]
public sealed class DirectionalLight : InfiniteLight
{
	public DirectionalLight() => DirectlyVisible = false;

	Float3 incidentDirection;
	RGB128 scaledIntensity;
	float cosAngle;

	float _angle = 0.6f;
	float _power;

	/// <summary>
	/// The angle in degrees at which light rays can deviate from the general <see cref="Float3.Forward"/> direction.
	/// </summary>
	/// <remarks>This is also half of the angular diameter, viewed from the scene.</remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if not within the range of [0, 90].</exception>
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

	public override bool IsDelta => !FastMath.Positive(1f - cosAngle); //Is delta light if cosAngle is basically one

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);

		//Precalculate parameters
		incidentDirection = (LocalToWorldRotation * Float3.Backward).Normalized;

		float radians = Scalars.ToRadians(Angle);
		cosAngle = MathF.Cos(radians);

		//Maintain consistent intensity regardless of the angle
		if (!IsDelta)
		{
			float scale = 0.5f - 0.5f * MathF.Cos(radians * 2f);
			scaledIntensity = Intensity / scale * Scalars.PiR;
		}
		else scaledIntensity = RGB128.Black;

		//Approximates that the total energy to be half of the scene's bounding disk area
		const float Multiplier = Scalars.Pi * 0.5f;
		float radius = scene.accelerator.SphereBound.radius;
		_power = Intensity.Luminance * Multiplier * radius * radius;
	}


	public override RGB128 Evaluate(Float3 incident)
	{
		Ensure.AreEqual(incident.SquaredMagnitude, 1f);
		if (IsDelta) return RGB128.Black;

		float cosIncident = incidentDirection.Dot(incident);
		if (cosIncident <= cosAngle) return RGB128.Black;

		Ensure.IsTrue(cosIncident <= 1f);
		return scaledIntensity;
	}

	public override float ProbabilityDensity(in GeometryPoint origin, Float3 incident)
	{
		if (IsDelta) return 0f;
		return Sample2D.UniformConePdf(cosAngle);
	}

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		travel = float.PositiveInfinity;

		if (IsDelta)
		{
			incident = incidentDirection;
			return (Intensity, 1f);
		}

		incident = sample.UniformCone(cosAngle);
		incident = LocalToWorldRotation * Utility.NegateZ(incident);
		return (scaledIntensity, Sample2D.UniformConePdf(cosAngle));
	}
}