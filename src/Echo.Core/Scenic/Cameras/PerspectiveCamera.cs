using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A standard <see cref="Camera"/> with perspective view similar to that of human eyes or real life camera.
/// </summary>
[EchoSourceUsable]
public sealed class PerspectiveCamera : Camera
{
	public PerspectiveCamera() => FieldOfView = 65f;

	float fieldOfView;
	float forwardLength;

	/// <summary>
	/// Horizontal field of view in degrees.
	/// </summary>
	[EchoSourceUsable]
	public float FieldOfView
	{
		get => fieldOfView;
		set
		{
			fieldOfView = value;
			forwardLength = 0.5f / MathF.Tan(Scalars.ToRadians(value) / 2f);
		}
	}

	public override Ray SpawnRay(in RaySpawner spawner, in CameraSample sample) => SpawnRay(spawner, sample.uv);

	public override Ray SpawnRay(in RaySpawner spawner) => SpawnRay(spawner, Float2.Half);

	Ray SpawnRay(in RaySpawner spawner, Float2 uv)
	{
		Float3 direction = spawner.SpawnX(uv).CreateXY(forwardLength);
		direction = InverseTransform.MultiplyDirection(direction);
		return new Ray(ContainedPosition, direction.Normalized);
	}
}