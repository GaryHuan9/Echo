using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A sensor through which a <see cref="Scene"/> can be evaluated.
/// </summary>
/// <remarks>The origin of <see cref="Ray"/>s.</remarks>
public class Camera : Entity
{
	public Camera(float fieldOfView) => FieldOfView = fieldOfView;

	float fieldOfView;
	float forwardLength;

	/// <summary>
	/// Horizontal field of view in degrees.
	/// </summary>
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

		return new Ray(Position, direction);
	}

	public void LookAt(Entity target) => LookAt(target.Position);

	public void LookAt(Float3 target)
	{
		Float3 to = (target - Position).Normalized;

		float yAngle = -Float2.Up.SignedAngle(to.XZ);
		float xAngle = -Float2.Right.SignedAngle(to.RotateXZ(yAngle).ZY);

		Rotation = new Float3(xAngle, yAngle, 0f);
	}
}