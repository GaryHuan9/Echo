using Echo.Core.Scenic.Cameras;

namespace Echo.Core.Evaluation.Sampling;

/// <summary>
/// A combination of <see cref="Sample2D"/> and <see cref="Sample1D"/> that defines the sampling for a <see cref="Camera"/>.
/// </summary>
public readonly struct CameraSample
{
	public CameraSample(Sample2D uv, Sample1D time)
	{
		this.uv = uv;
		this.time = time;
	}

	public readonly Sample2D uv;
	public readonly Sample1D time;

	public static CameraSample Create(ContinuousDistribution distribution) => new
	(
		distribution.Next2D(),
		distribution.Next1D()
	);
}