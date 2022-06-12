using System;

namespace Echo.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// A uniform <see cref="ContinuousDistribution"/> that returns purely random values.
/// </summary>
public record UniformDistribution : ContinuousDistribution
{
	public UniformDistribution() { }

	protected UniformDistribution(UniformDistribution source) : base(source) { }

	protected override Sample1D Next1DImpl() => (Sample1D)Prng.Next1();

	protected override Sample2D Next2DImpl() => (Sample2D)Prng.Next2();

	protected override void FillSpan1D(Span<Sample1D> samples)
	{
		foreach (ref Sample1D sample in samples) sample = Next1DImpl();
	}

	protected override void FillSpan2D(Span<Sample2D> samples)
	{
		foreach (ref Sample2D sample in samples) sample = Next2DImpl();
	}
}