namespace EchoRenderer.Rendering.Sampling
{
	public abstract class LimitedSampler : Sampler
	{
		protected LimitedSampler(int sampleCount, int dimensionCount) : base(sampleCount)
		{
			ones = new SpanAggregate<Sample1>[dimensionCount];
			twos = new SpanAggregate<Sample2>[dimensionCount];

			for (int i = 0; i < dimensionCount; i++)
			{
				ones[i] = new SpanAggregate<Sample1>(1, sampleCount);
				twos[i] = new SpanAggregate<Sample2>(1, sampleCount);
			}
		}

		protected LimitedSampler(LimitedSampler sampler) : base(sampler)
		{
			ones = new SpanAggregate<Sample1>[sampler.ones.Length];
			twos = new SpanAggregate<Sample2>[sampler.twos.Length];
		}

		protected readonly SpanAggregate<Sample1>[] ones;
		protected readonly SpanAggregate<Sample2>[] twos;

		int oneIndex;
		int twoIndex;

		public override void BeginSample()
		{
			base.BeginSample();

			oneIndex = -1;
			twoIndex = -1;
		}

		public override Sample1 NextOne() => ones[++oneIndex].array[SampleIndex];
		public override Sample2 NextTwo() => twos[++twoIndex].array[SampleIndex];
	}
}