using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions;

public class UniformDistribution : ContinuousDistribution
{
	public UniformDistribution(int extend) : base(extend) { }

	UniformDistribution(UniformDistribution source) : base(source) => Jitter = source.Jitter;

	/// <summary>
	/// Whether the <see cref="ContinuousDistribution"/> randomly varies.
	/// </summary>
	public bool Jitter { get; set; } = true;

	public override void BeginSample()
	{
		base.BeginSample();
		Assert.IsFalse(Jitter && Random == null); //Make sure that if jitter is true, the prng is not null

		foreach (SpanAggregate<Sample1D> aggregate in arrayOnes)
		foreach (ref Sample1D sample in aggregate[SampleNumber])
		{
			sample = (Sample1D)(Jitter ? Random.Next1() : 0.5f);
		}

		foreach (SpanAggregate<Sample2D> aggregate in arrayTwos)
		foreach (ref Sample2D sample in aggregate[SampleNumber])
		{
			sample = (Sample2D)(Jitter ? Random.Next2() : Float2.half);
		}
	}

	public override Sample1D Next1D() => (Sample1D)(Jitter ? Random.Next1() : 0.5f);
	public override Sample2D Next2D() => (Sample2D)(Jitter ? Random.Next2() : Float2.half);

	public override ContinuousDistribution Replicate() => new UniformDistribution(this);
}