using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public class UniformDistribution : Distribution
	{
		public UniformDistribution(int sampleCount) : base(sampleCount) { }

		UniformDistribution(UniformDistribution distribution) : base(distribution) { }

		/// <summary>
		/// Whether the <see cref="Distribution"/> randomly varies.
		/// </summary>
		public bool Jitter { get; set; } = true;

		public override void BeginSample()
		{
			base.BeginSample();
			Assert.IsFalse(Jitter && PRNG == null); //Make sure that if jitter is true, the prng is not null

			foreach (SpanAggregate<Distro1> aggregate in arrayOnes)
			foreach (ref Distro1 distro in aggregate[SampleIndex])
			{
				distro = Jitter ? new Distro1(PRNG.Next1()) : new Distro1(0.5f);
			}

			foreach (SpanAggregate<Distro2> aggregate in arrayTwos)
			foreach (ref Distro2 distro in aggregate[SampleIndex])
			{
				distro = Jitter ? new Distro2(PRNG.Next2()) : new Distro2(Float2.half);
			}
		}

		public override Distro1 NextOne() => new(PRNG.Next1());
		public override Distro2 NextTwo() => new(PRNG.Next2());

		public override Distribution Replicate() => new UniformDistribution(this);
	}
}