using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A standard <see cref="Camera"/> with perspective view similar to that of human eyes or real life camera.
/// </summary>
[EchoSourceUsable]
public sealed class PerspectiveCamera : Camera
{
	float forwardLength;
	bool hasDepthOfField;
	float focusScale;

	/// <summary>
	/// Horizontal field of view in degrees.
	/// </summary>
	[EchoSourceUsable]
	public float FieldOfView { get; set; } = 65f;

	/// <summary>
	/// The radius of the lens (in photography, the aperture), controlling the intensity of the depth of field.
	/// </summary>
	/// <remarks>This is usually a small number, such as 0.01. If this is zero or negative, depth of field is disabled.</remarks>
	[EchoSourceUsable]
	public float LensRadius { get; set; } = 0.01f;

	/// <summary>
	/// The distance at which this <see cref="PerspectiveCamera"/> produces a perfectly clear image.
	/// </summary>
	/// <remarks>If this is zero or negative, depth of field is disabled.</remarks>
	[EchoSourceUsable]
	public float FocalDistance { get; set; }

	public override void Prepare()
	{
		base.Prepare();

		forwardLength = 0.5f / MathF.Tan(Scalars.ToRadians(FieldOfView) / 2f);
		hasDepthOfField = FastMath.Positive(LensRadius) && FastMath.Positive(FocalDistance);

		if (hasDepthOfField) focusScale = FocalDistance / forwardLength;
	}

	public override Ray SpawnRay(in RaySpawner spawner, CameraSample sample)
	{
		if (!hasDepthOfField) return SpawnRay(spawner, sample.shift);

		Float2 lens = sample.lens.ConcentricDisk * LensRadius;
		Float2 uv = spawner.SpawnX(sample.shift) * focusScale;
		Float3 focus = uv.CreateXY(FocalDistance);

		Float3 origin = lens.CreateXY();
		Float3 direction = focus - origin;

		return new Ray
		(
			InverseTransform.MultiplyPoint(origin),
			InverseTransform.MultiplyDirection(direction).Normalized
		);
	}

	public override Ray SpawnRay(in RaySpawner spawner) => SpawnRay(spawner, Float2.Half);

	/// <summary>
	/// Identical to <see cref="Focus(Float3)"/>, except focuses to an <see cref="Entity"/>.
	/// </summary>
	[EchoSourceUsable]
	public void Focus(Entity target)
	{
		if (target.Root == Root) Focus(target.RootedPosition);
		throw SceneException.AmbiguousTransform(target);
	}

	/// <summary>
	/// Changes <see cref="FocalDistance"/> to focus a particular point.
	/// </summary>
	/// <remarks>If <see cref="FocalDistance"/> is negative, its absolute value is used instead.</remarks>
	[EchoSourceUsable]
	public void Focus(Float3 target)
	{
		Float3 offset = target - RootedPosition;
		Float3 forward = RootedRotation * Float3.Forward;
		FocalDistance = FastMath.Abs(offset.Dot(forward));
	}

	Ray SpawnRay(in RaySpawner spawner, Float2 shift)
	{
		Float3 direction = spawner.SpawnX(shift).CreateXY(forwardLength);
		direction = InverseTransform.MultiplyDirection(direction);
		return new Ray(RootedPosition, direction.Normalized);
	}
}