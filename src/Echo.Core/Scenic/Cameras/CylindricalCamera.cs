using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A <see cref="Camera"/> that captures all direction around one point using equirectangular projection.
/// </summary>
/// <remarks>Since they use identical projection methods, the output of this <see cref="CylindricalCamera"/>
/// can be directly used and viewed in a <see cref="Textures.Directional.CylindricalTexture"/>.</remarks>
[EchoSourceUsable]
public sealed class CylindricalCamera : Camera
{
	Float3x3 rotationMatrix;

	public override Ray SpawnRay(in RaySpawner spawner, in CameraSample sample) => SpawnRay(spawner, sample.uv);

	public override Ray SpawnRay(in RaySpawner spawner) => SpawnRay(spawner, Float2.Half);

	protected override void OnTransformRecalculated()
	{
		base.OnTransformRecalculated();
		rotationMatrix = (Float3x3)ContainedRotation;
	}

	Ray SpawnRay(in RaySpawner spawner, Float2 uv)
	{
		uv = (spawner.position + uv) * spawner.sizeR;

		Float3 direction = CylindricalTexture.ToDirection(uv);
		direction = (rotationMatrix * direction).Normalized;
		return new Ray(ContainedPosition, direction);
	}
}