using Echo.Core.Common.Packed;

namespace Echo.Core.Evaluation.Sampling;

/// <summary>
/// An abstract <see cref="ContinuousDistribution"/> that allows pixel samples to be drawn out of their conventional order.
/// </summary>
public abstract record OutOfOrderDistribution : ContinuousDistribution
{
	protected OutOfOrderDistribution() { }

	protected OutOfOrderDistribution(OutOfOrderDistribution source) : base(source) { }

	public override void BeginSeries(Int2 position)
	{
		base.BeginSeries(position);
	}

	public override void BeginSession()
	{
		base.BeginSession();
	}
}