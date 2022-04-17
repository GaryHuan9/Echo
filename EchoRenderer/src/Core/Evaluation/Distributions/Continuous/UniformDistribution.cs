using System;
using CodeHelpers.Packed;

namespace EchoRenderer.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// A uniform <see cref="ContinuousDistribution"/>. Either returns purely random values
/// if <see cref="ContinuousDistribution.Prng"/> is assigned, or simply return just 1/2.
/// </summary>
public class UniformDistribution : ContinuousDistribution
{
	public UniformDistribution(int extend) : base(extend) { }

	UniformDistribution(UniformDistribution source) : base(source) { }

	public override ContinuousDistribution Replicate() => new UniformDistribution(this);

	protected override Sample1D Next1DCore()
	{
		if (Prng == null) return (Sample1D)0.5f;
		return (Sample1D)Prng.Next1();
	}

	protected override Sample2D Next2DCore()
	{
		if (Prng == null) return (Sample2D)Float2.Half;
		return (Sample2D)Prng.Next2();
	}

	protected override void FillSpan1D(Span<Sample1D> samples)
	{
		foreach (ref Sample1D sample in samples) sample = Next1DCore();
	}

	protected override void FillSpan2D(Span<Sample2D> samples)
	{
		foreach (ref Sample2D sample in samples) sample = Next2DCore();
	}
}