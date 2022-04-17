using CodeHelpers.Packed;

namespace EchoRenderer.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// An abstract <see cref="ContinuousDistribution"/> that allows pixel samples to be drawn out of their conventional order.
/// </summary>
public abstract class OutOfOrderDistribution : ContinuousDistribution
{
	protected OutOfOrderDistribution(int extend) : base(extend) { }
	protected OutOfOrderDistribution(OutOfOrderDistribution source) : base(source) { }

	public override void BeginPixel(Int2 position)
	{
		base.BeginPixel(position);
	}

	public override void BeginSample()
	{
		base.BeginSample();
	}
}