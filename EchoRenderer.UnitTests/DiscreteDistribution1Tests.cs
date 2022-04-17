using System.Collections.Generic;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Evaluation.Distributions;
using EchoRenderer.Core.Evaluation.Distributions.Discrete;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class DiscreteDistribution1Tests
{
	[SetUp]
	public void SetUp()
	{
		constant = new DiscreteDistribution1D(stackalloc[] { 1f, 1f, 1f, 1f, 1f });
		singular = new DiscreteDistribution1D(stackalloc[] { 4f });
		sequence = new DiscreteDistribution1D(stackalloc[] { 1f, 2f, 3f });
		allZeros = new DiscreteDistribution1D(stackalloc[] { 0f, 0f, 0f });
		zerosOne = new DiscreteDistribution1D(stackalloc[] { 0f, 0f, 0f, 1f });
		oneZeros = new DiscreteDistribution1D(stackalloc[] { 1f, 0f, 0f, 0f });

		array = new[] { constant, singular, sequence, allZeros, zerosOne, oneZeros };
	}

	DiscreteDistribution1D constant;
	DiscreteDistribution1D singular;
	DiscreteDistribution1D sequence;
	DiscreteDistribution1D allZeros;
	DiscreteDistribution1D zerosOne;
	DiscreteDistribution1D oneZeros;

	DiscreteDistribution1D[] array;

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
	public void ProbabilityDensity([Random(0f, 1f, 1000)] float random)
	{
		Sample1D sample = (Sample1D)random;

		foreach (DiscreteDistribution1D distribution in array) ProbabilityDensitySingle(distribution, sample);
	}

	[Test]
	public void ProbabilityDensityBoundaries()
	{
		foreach (DiscreteDistribution1D distribution in array)
		foreach (Sample1D sample in Uniform(distribution.Count))
		{
			ProbabilityDensitySingle(distribution, sample);
		}
	}

	static void ProbabilityDensitySingle(DiscreteDistribution1D distribution, Sample1D sample)
	{
		var one = distribution.Sample(sample);
		var two = distribution.Find(sample);

		Assert.That(distribution.ProbabilityDensity(one), Is.EqualTo(one.pdf).Roughly());
		Assert.That(distribution.ProbabilityDensity(two), Is.EqualTo(two.pdf).Roughly());

		Assert.That(one.pdf, Is.Not.Zero);
		Assert.That(two.pdf, Is.Not.Zero);
	}

	static IEnumerable<Sample1D> Uniform(int count)
	{
		double countR = 1d / count;

		for (int i = 0; i <= count; i++) yield return (Sample1D)(i * countR);
	}
}