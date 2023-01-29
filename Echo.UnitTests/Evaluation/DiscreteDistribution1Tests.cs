using System;
using System.Collections.Generic;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using NUnit.Framework;

namespace Echo.UnitTests.Evaluation;

[TestFixture]
public class DiscreteDistribution1Tests
{
	static DiscreteDistribution1Tests()
	{
		constant = new DiscreteDistribution1D(stackalloc[] { 1f, 1f, 1f, 1f, 1f });
		singular = new DiscreteDistribution1D(stackalloc[] { 4f });
		sequence = new DiscreteDistribution1D(stackalloc[] { 1f, 2f, 3f });
		allZeros = new DiscreteDistribution1D(stackalloc[] { 0f, 0f, 0f });
		zerosOne = new DiscreteDistribution1D(stackalloc[] { 0f, 0f, 0f, 1f });
		oneZeros = new DiscreteDistribution1D(stackalloc[] { 1f, 0f, 0f, 0f });

		array = new[] { constant, singular, sequence, allZeros, zerosOne, oneZeros };

		var distribution = new StratifiedDistribution
		{
			Extend = samples.Length,
			Prng = new SystemPrng(1)
		};

		distribution.BeginSeries(Int2.Zero);

		foreach (ref Sample1D sample in samples.AsSpan())
		{
			distribution.BeginSession();
			sample = distribution.Next1D();
		}
	}

	static readonly DiscreteDistribution1D constant;
	static readonly DiscreteDistribution1D singular;
	static readonly DiscreteDistribution1D sequence;
	static readonly DiscreteDistribution1D allZeros;
	static readonly DiscreteDistribution1D zerosOne;
	static readonly DiscreteDistribution1D oneZeros;

	static readonly DiscreteDistribution1D[] array;
	static readonly Sample1D[] samples = new Sample1D[1000];

	[Test]
	public void Sum()
	{
		Assert.That(constant.sum, Is.EqualTo(5f).Roughly());
		Assert.That(singular.sum, Is.EqualTo(4f).Roughly());
		Assert.That(sequence.sum, Is.EqualTo(6f).Roughly());
		Assert.That(allZeros.sum, Is.EqualTo(0f).Roughly());
		Assert.That(zerosOne.sum, Is.EqualTo(1f).Roughly());
		Assert.That(oneZeros.sum, Is.EqualTo(1f).Roughly());
	}

	[Test]
	public void Integral()
	{
		Assert.That(constant.integral, Is.EqualTo(1f).Roughly());
		Assert.That(singular.integral, Is.EqualTo(4f).Roughly());
		Assert.That(sequence.integral, Is.EqualTo(2f).Roughly());
		Assert.That(allZeros.integral, Is.EqualTo(0f).Roughly());
		Assert.That(zerosOne.integral, Is.EqualTo(0.25f).Roughly());
		Assert.That(oneZeros.integral, Is.EqualTo(0.25f).Roughly());
	}

	[Test]
	public void Count()
	{
		Assert.That(constant, Has.Count.EqualTo(5));
		Assert.That(singular, Has.Count.EqualTo(1));
		Assert.That(sequence, Has.Count.EqualTo(3));
		Assert.That(allZeros, Has.Count.EqualTo(3));
		Assert.That(zerosOne, Has.Count.EqualTo(4));
		Assert.That(oneZeros, Has.Count.EqualTo(4));
	}

	[Test]
	public void Probability([ValueSource(nameof(array))] DiscreteDistribution1D distribution)
	{
		foreach (Sample1D sample in samples) ProbabilitySingle(distribution, sample);
	}

	[Test]
	public void ProbabilityBoundaries([ValueSource(nameof(array))] DiscreteDistribution1D distribution)
	{
		foreach (Sample1D sample in Uniform(distribution.Count)) ProbabilitySingle(distribution, sample);
	}

	static void ProbabilitySingle(DiscreteDistribution1D distribution, Sample1D sample)
	{
		var continuous = distribution.Sample(sample);
		var discrete = distribution.Pick(sample);

		Assert.That(distribution.ProbabilityDensity(continuous), Is.EqualTo(continuous.pdf).Roughly());
		Assert.That(distribution.ProbabilityMass(discrete), Is.EqualTo(discrete.pdf).Roughly());

		Assert.That(continuous.pdf, Is.Not.Zero);
		Assert.That(discrete.pdf, Is.Not.Zero);

		var again = distribution.Pick(ref sample);
		Assert.That(again, Is.EqualTo(discrete));
	}

	static IEnumerable<Sample1D> Uniform(int count)
	{
		decimal countR = 1m / count;

		for (int i = 0; i <= count; i++) yield return (Sample1D)(float)(i * countR);
	}
}