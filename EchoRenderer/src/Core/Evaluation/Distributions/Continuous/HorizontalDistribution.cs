using CodeHelpers.Packed;

namespace EchoRenderer.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// An abstract <see cref="ContinuousDistribution"/> that draws single samples horizontally across different pixel samples
/// by using either <see cref="ContinuousDistribution.NextSpan1D"/> or <see cref="ContinuousDistribution.NextSpan2D"/>.
/// </summary>
public abstract class HorizontalDistribution : ContinuousDistribution
{
	protected HorizontalDistribution(int extend) : base(extend) { }

	protected HorizontalDistribution(HorizontalDistribution source) : base(source) { }

	readonly BufferDomain<Sample1D> singles1D = new();
	readonly BufferDomain<Sample2D> singles2D = new();

	public override void BeginPixel(Int2 position)
	{
		base.BeginPixel(position);

		singles1D.Reset(false);
		singles2D.Reset(false);
	}

	public override void BeginSample()
	{
		base.BeginSample();

		singles1D.Reset(true);
		singles2D.Reset(true);
	}

	protected override Sample1D Next1DCore()
	{
		if (singles1D.TryFetch(extend, out var buffer)) FillSpan1D(buffer);

		return buffer[SampleNumber];
	}

	protected override Sample2D Next2DCore()
	{
		if (singles2D.TryFetch(extend, out var buffer)) FillSpan2D(buffer);

		return buffer[SampleNumber];
	}
}