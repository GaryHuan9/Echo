using EchoRenderer.Rendering.Distributions;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public class Piecewise1Test
{
	[SetUp]
	public void Setup()
	{
		constant = new Piecewise1(stackalloc[] { 1f, 1f, 1f, 1f, 1f });
		linear = new Piecewise1(stackalloc[] { 1f, 2f, 3f, 4f });
	}

	Piecewise1 constant;
	Piecewise1 linear;

	[Test]
	public void Sum()
	{
		Assert.AreEqual(constant.sum, 5f);
		Assert.AreEqual(linear.sum, 10f);
	}

	[Test]
	public void Integral()
	{
		Assert.AreEqual(constant.integral, 1f);
		Assert.AreEqual(linear.integral, 2.5f);
	}

	[Test]
	public void Length()
	{
		Assert.AreEqual(constant.Length, 5);
		Assert.AreEqual(linear.Length, 4);
	}
}