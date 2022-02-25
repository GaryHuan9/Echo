using System.Collections.Generic;
using EchoRenderer.Core.Rendering.Distributions;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class Piecewise1Tests
{
	[SetUp]
	public void SetUp()
	{
		constant = new Piecewise1(stackalloc[] { 1f, 1f, 1f, 1f, 1f });
		singular = new Piecewise1(stackalloc[] { 4f });
		sequence = new Piecewise1(stackalloc[] { 1f, 2f, 3f });
		allZeros = new Piecewise1(stackalloc[] { 0f, 0f, 0f });
		zerosOne = new Piecewise1(stackalloc[] { 0f, 0f, 0f, 1f });
		oneZeros = new Piecewise1(stackalloc[] { 1f, 0f, 0f, 0f });

		array = new[] { constant, singular, sequence, allZeros, zerosOne, oneZeros };
	}

	Piecewise1 constant;
	Piecewise1 singular;
	Piecewise1 sequence;
	Piecewise1 allZeros;
	Piecewise1 zerosOne;
	Piecewise1 oneZeros;

	Piecewise1[] array;

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
		Distro1 distro = (Distro1)random;

		foreach (Piecewise1 piecewise in array) ProbabilityDensitySingle(piecewise, distro);
	}

	[Test]
	public void ProbabilityDensityBoundaries()
	{
		foreach (Piecewise1 piecewise in array)
		foreach (Distro1 distro in Uniform(piecewise.Count))
		{
			ProbabilityDensitySingle(piecewise, distro);
		}
	}

	static void ProbabilityDensitySingle(Piecewise1 piecewise, Distro1 distro)
	{
		Assert.That(piecewise.ProbabilityDensity(piecewise.Find(distro, out float pdf0)), Is.EqualTo(pdf0).Roughly());
		Assert.That(piecewise.ProbabilityDensity(piecewise.Sample(distro, out float pdf1)), Is.EqualTo(pdf1).Roughly());

		Assert.That(pdf0, Is.Not.Zero);
		Assert.That(pdf1, Is.Not.Zero);
	}

	static IEnumerable<Distro1> Uniform(int count)
	{
		double countR = 1d / count;

		for (int i = 0; i <= count; i++) yield return (Distro1)(i * countR);
	}
}