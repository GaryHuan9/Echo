namespace EchoRenderer.Rendering.Sampling
{
	public abstract class LimitedSampler : Sampler
	{
		protected LimitedSampler(int sampleCount, int dimensionCount) : base(sampleCount)
		{
			singleOnes = new SpanAggregate<Sample1>[dimensionCount];
			singleTwos = new SpanAggregate<Sample2>[dimensionCount];

			for (int i = 0; i < dimensionCount; i++)
			{
				singleOnes[i] = new SpanAggregate<Sample1>(1, sampleCount);
				singleTwos[i] = new SpanAggregate<Sample2>(1, sampleCount);
			}
		}

		protected LimitedSampler(LimitedSampler sampler) : base(sampler)
		{
			singleOnes = new SpanAggregate<Sample1>[sampler.singleOnes.Length];
			singleTwos = new SpanAggregate<Sample2>[sampler.singleTwos.Length];
		}

		protected readonly SpanAggregate<Sample1>[] singleOnes;
		protected readonly SpanAggregate<Sample2>[] singleTwos;

		int singleOneIndex;
		int singleTwoIndex;

		public override void BeginSample()
		{
			base.BeginSample();

			singleOneIndex = -1;
			singleTwoIndex = -1;
		}

		public override Sample1 NextOne() => singleOnes[++singleOneIndex].array[SampleIndex];
		public override Sample2 NextTwo() => singleTwos[++singleTwoIndex].array[SampleIndex];
	}
}