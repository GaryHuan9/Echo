namespace EchoRenderer.Rendering.Distributions
{
	public abstract class LimitedDistribution : Distribution
	{
		protected LimitedDistribution(int sampleCount, int dimensionCount) : base(sampleCount)
		{
			singleOnes = new SpanAggregate<Distro1>[dimensionCount];
			singleTwos = new SpanAggregate<Distro2>[dimensionCount];

			for (int i = 0; i < dimensionCount; i++)
			{
				singleOnes[i] = new SpanAggregate<Distro1>(1, sampleCount);
				singleTwos[i] = new SpanAggregate<Distro2>(1, sampleCount);
			}
		}

		protected LimitedDistribution(LimitedDistribution distribution) : base(distribution)
		{
			singleOnes = new SpanAggregate<Distro1>[distribution.singleOnes.Length];
			singleTwos = new SpanAggregate<Distro2>[distribution.singleTwos.Length];
		}

		protected readonly SpanAggregate<Distro1>[] singleOnes;
		protected readonly SpanAggregate<Distro2>[] singleTwos;

		int singleOneIndex;
		int singleTwoIndex;

		public override void BeginSample()
		{
			base.BeginSample();

			singleOneIndex = -1;
			singleTwoIndex = -1;
		}

		public override Distro1 NextOne() => singleOnes[++singleOneIndex].array[SampleIndex];
		public override Distro2 NextTwo() => singleTwos[++singleTwoIndex].array[SampleIndex];
	}
}