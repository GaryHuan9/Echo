using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public class UniformSampler : Sampler
	{
		public UniformSampler(int sampleCount) : base(sampleCount) { }

		UniformSampler(UniformSampler sampler) : base(sampler) { }

		public override void BeginPixel(Int2 position)
		{
			base.BeginPixel(position);
			Assert.IsNotNull(PRNG);

			foreach (SpanAggregate<Sample1> aggregate in arrayOnes)
			foreach (ref Sample1 sample in aggregate.array.AsSpan())
			{
				sample = new Sample1(PRNG.Next1());
			}

			foreach (SpanAggregate<Sample2> aggregate in arrayTwos)
			foreach (ref Sample2 sample in aggregate.array.AsSpan())
			{
				sample = new Sample2(PRNG.Next2());
			}
		}

		public override Sample1 NextOne() => new(PRNG.Next1());
		public override Sample2 NextTwo() => new(PRNG.Next2());

		public override Sampler Replicate() => new UniformSampler(this);
	}
}