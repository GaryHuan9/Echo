﻿using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions;

public class UniformDistribution : Distribution
{
	public UniformDistribution(int sampleCount) : base(sampleCount) { }

	UniformDistribution(UniformDistribution distribution) : base(distribution) => Jitter = distribution.Jitter;

	/// <summary>
	/// Whether the <see cref="Distribution"/> randomly varies.
	/// </summary>
	public bool Jitter { get; set; } = true;

	public override void BeginSample()
	{
		base.BeginSample();
		Assert.IsFalse(Jitter && Random == null); //Make sure that if jitter is true, the prng is not null

		foreach (SpanAggregate<Distro1> aggregate in arrayOnes)
		foreach (ref Distro1 distro in aggregate[SampleIndex])
		{
			distro = (Distro1)(Jitter ? Random.Next1() : 0.5f);
		}

		foreach (SpanAggregate<Distro2> aggregate in arrayTwos)
		foreach (ref Distro2 distro in aggregate[SampleIndex])
		{
			distro = (Distro2)(Jitter ? Random.Next2() : Float2.half);
		}
	}

	public override Distro1 NextOne() => (Distro1)(Jitter ? Random.Next1() : 0.5f);
	public override Distro2 NextTwo() => (Distro2)(Jitter ? Random.Next2() : Float2.half);

	public override Distribution Replicate() => new UniformDistribution(this);
}