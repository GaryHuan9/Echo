using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A <see cref="Camera"/> through which objects appear the same size regardless of their distance.
/// </summary>
[EchoSourceUsable]
public class OrthographicCamera : Camera
{
	/// <summary>
	/// The horizontal size of the orthographic view.
	/// </summary>
	/// <remarks>An object of this size at the center of the view will
	/// completely fill this <see cref="OrthographicCamera"/>.</remarks>
	[EchoSourceUsable]
	public float Width { get; set; } = 8f;

	Float3 direction;

	public override void Prepare()
	{
		base.Prepare();
		direction = ContainedRotation * Float3.Forward;
	}

	public override Ray SpawnRay(in RaySpawner spawner, CameraSample sample) => SpawnRay(spawner, sample.shift);

	public override Ray SpawnRay(in RaySpawner spawner) => SpawnRay(spawner, Float2.Half);

	Ray SpawnRay(in RaySpawner spawner, Float2 shift)
	{
		Float2 uv = spawner.SpawnX(shift) * Width;
		Float3 origin = new Float3(uv.X, uv.Y, 0f);
		return new Ray(InverseTransform.MultiplyPoint(origin), direction);
	}
}