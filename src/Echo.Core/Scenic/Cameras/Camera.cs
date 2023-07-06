using Echo.Core.Aggregation.Primitives;
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
public abstract class Camera : Entity
{
	/// <summary>
	/// An optional identifier for this <see cref="Camera"/>.
	/// </summary>
	[EchoSourceUsable]
	public string Name { get; set; }

	/// <summary>
	/// Invoked before rendering to initialize this <see cref="Camera"/> and prepare it for rendering.
	/// </summary>
	public virtual void Prepare() { }

	/// <summary>
	/// Spawns a <see cref="Ray"/> from this <see cref="Camera"/>, using a <see cref="CameraSample"/>.
	/// </summary>
	public abstract Ray SpawnRay(in RaySpawner spawner, CameraSample sample);

	/// <summary>
	/// Spawns a <see cref="Ray"/> from this <see cref="Camera"/>, without a <see cref="CameraSample"/>.
	/// </summary>
	public abstract Ray SpawnRay(in RaySpawner spawner);

	/// <summary>
	/// Identical to <see cref="LookAt(Float3)"/>, except points to an <see cref="Entity"/>.
	/// </summary>
	[EchoSourceUsable]
	public void LookAt(Entity target)
	{
		if (target.Root == Root) LookAt(target.ContainedPosition);
		throw SceneException.AmbiguousTransform(target);
	}

	/// <summary>
	/// Points the local <see cref="Float3.Forward"/> direction at a position in world space.
	/// </summary>
	[EchoSourceUsable]
	public void LookAt(Float3 target)
	{
		Float3 direction = (target - ContainedPosition).Normalized;

		float yAngle = -Float2.Up.SignedAngle(direction.XZ);
		float xAngle = -Float2.Right.SignedAngle(direction.RotateXZ(yAngle).ZY);

		Rotation = new Versor(xAngle, yAngle, 0f);
	}

	protected override void CheckRoot(EntityPack root)
	{
		base.CheckRoot(root);
		if (root is not Scene) throw SceneException.RootNotScene(nameof(Camera));
	}
}