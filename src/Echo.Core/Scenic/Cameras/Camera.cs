using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A sensor through which a <see cref="Scene"/> can be evaluated.
/// </summary>
/// <remarks>The origin of <see cref="Ray"/>s.</remarks>
[EchoSourceUsable]
public class Camera : Entity
{
	[EchoSourceUsable]
	public Camera(float fieldOfView = 60f) => FieldOfView = fieldOfView;

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

	/// <summary>
	/// Spawns a <see cref="Ray"/> from this <see cref="Camera"/>.
	/// </summary>
	public Ray SpawnRay(in CameraSample sample, in RaySpawner spawner)
	{
		Float3 direction = spawner.SpawnX(sample.uv).CreateXY(forwardLength);
		direction = InverseTransform.MultiplyDirection(direction).Normalized;

		return new Ray(ContainedPosition, direction);
	}

	public void LookAt(Entity target) => LookAt(target.ContainedPosition);

	public void LookAt(Float3 target)
	{
		Float3 to = (target - ContainedPosition).Normalized;

		float yAngle = -Float2.Up.SignedAngle(to.XZ);
		float xAngle = -Float2.Right.SignedAngle(to.RotateXZ(yAngle).ZY);

		Rotation = new Versor(xAngle, yAngle, 0f);
	}
}