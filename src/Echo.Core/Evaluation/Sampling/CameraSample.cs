using Echo.Core.Scenic.Cameras;

namespace Echo.Core.Evaluation.Sampling;

/// <summary>
/// A combination of <see cref="Sample2D"/> and <see cref="Sample1D"/> that defines the sampling for a <see cref="Camera"/>.
/// </summary>
public readonly struct CameraSample
{
	public CameraSample(Sample2D shift, Sample2D lens)
	{
		this.shift = shift;
		this.lens = lens;
	}

	public readonly Sample2D shift;
	public readonly Sample2D lens;

	public static CameraSample Create(ContinuousDistribution distribution) => new
	(
		distribution.Next2D(),
		distribution.Next2D()
	);
}