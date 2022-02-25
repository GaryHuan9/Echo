namespace EchoRenderer.Core.Rendering.Distributions;

public abstract class LimitedDistribution : ContinuousDistribution
{
	protected LimitedDistribution(int sampleCount, int dimensionCount) : base(sampleCount)
	{
		singleOnes = new SpanAggregate<Sample1D>[dimensionCount];
		singleTwos = new SpanAggregate<Sample2D>[dimensionCount];

		for (int i = 0; i < dimensionCount; i++)
		{
			singleOnes[i] = new SpanAggregate<Sample1D>(1, sampleCount);
			singleTwos[i] = new SpanAggregate<Sample2D>(1, sampleCount);
		}
	}

	protected LimitedDistribution(LimitedDistribution distribution) : base(distribution)
	{
		singleOnes = new SpanAggregate<Sample1D>[distribution.singleOnes.Length];
		singleTwos = new SpanAggregate<Sample2D>[distribution.singleTwos.Length];
	}

	protected readonly SpanAggregate<Sample1D>[] singleOnes;
	protected readonly SpanAggregate<Sample2D>[] singleTwos;

	int singleOneIndex;
	int singleTwoIndex;

	public override void BeginSample()
	{
		base.BeginSample();

		singleOneIndex = -1;
		singleTwoIndex = -1;
	}

	public override Sample1D Next1D() => singleOnes[++singleOneIndex].array[SampleIndex];
	public override Sample2D Next2D() => singleTwos[++singleTwoIndex].array[SampleIndex];
}