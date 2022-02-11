using System;
using EchoRenderer.Rendering.Distributions;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public class Piecewise1Test
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		distros = new Distro1[4];
		FillUniform(distros);
	}

	[SetUp]
	public void SetUp()
	{
		constant = new Piecewise1(stackalloc[] { 1f, 1f, 1f, 1f, 1f });
		singular = new Piecewise1(stackalloc[] { 4f });
		sequence = new Piecewise1(stackalloc[] { 1f, 2f, 3f });
	}

	Distro1[] distros;

	Piecewise1 constant;
	Piecewise1 singular;
	Piecewise1 sequence;

	[Test]
	public void Sum()
	{
		Assert.That(constant.sum, Is.EqualTo(5f).Roughly());
		Assert.That(singular.sum, Is.EqualTo(4f).Roughly());
		Assert.That(sequence.sum, Is.EqualTo(6f).Roughly());
	}

	[Test]
	public void Integral()
	{
		Assert.That(constant.integral, Is.EqualTo(1f).Roughly());
		Assert.That(singular.integral, Is.EqualTo(4f).Roughly());
		Assert.That(sequence.integral, Is.EqualTo(2f).Roughly());
	}

	[Test]
	public void Count()
	{
		Assert.That(constant, Has.Count.EqualTo(5));
		Assert.That(singular, Has.Count.EqualTo(1));
		Assert.That(sequence, Has.Count.EqualTo(3));
	}

	[Test]
	public void ProbabilityDensity([Random(0f, 1f, 10)] float random)
	{
		Distro1 distro = (Distro1)random;

		foreach (Piecewise1 piecewise in new[] { constant, singular, sequence })
		{
			Assert.That(piecewise.ProbabilityDensity(piecewise.SampleDiscrete(distro, out float pdf)), Is.EqualTo(pdf).Roughly());
			Assert.That(piecewise.ProbabilityDensity(piecewise.SampleContinuous(distro, out pdf)), Is.EqualTo(pdf).Roughly());
		}
	}

	static void FillUniform(Span<Distro1> span)
	{
		double lengthR = 1d / (span.Length - 1d);

		for (int i = 0; i < span.Length; i++) span[i] = (Distro1)(i * lengthR);
	}
}