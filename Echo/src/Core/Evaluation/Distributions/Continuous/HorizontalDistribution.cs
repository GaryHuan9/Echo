using CodeHelpers.Packed;

namespace Echo.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// An abstract <see cref="ContinuousDistribution"/> that draws single samples horizontally across different pixel samples
/// by using either <see cref="ContinuousDistribution.NextSpan1D"/> or <see cref="ContinuousDistribution.NextSpan2D"/>.
/// </summary>
public abstract record HorizontalDistribution : ContinuousDistribution
{
	protected HorizontalDistribution() { }

	protected HorizontalDistribution(HorizontalDistribution source) : base(source)
	{
		singles1D = new BufferDomain<Sample1D>();
		singles2D = new BufferDomain<Sample2D>();
	}

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

	protected override Sample1D Next1DImpl()
	{
		if (singles1D.TryFetch(Extend, out var buffer)) FillSpan1D(buffer);

		return buffer[SampleNumber];
	}

	protected override Sample2D Next2DImpl()
	{
		if (singles2D.TryFetch(Extend, out var buffer)) FillSpan2D(buffer);

		return buffer[SampleNumber];
	}
}