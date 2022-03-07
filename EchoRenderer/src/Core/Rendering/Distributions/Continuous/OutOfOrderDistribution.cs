using CodeHelpers.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions.Continuous;

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