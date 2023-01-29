using System;
using System.Collections.Generic;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using NUnit.Framework;

namespace Echo.UnitTests.Textures;

[TestFixture]
public class SystemSerializerTests
{
	static SystemSerializerTests()
	{
		inputs.Add(Float4.Zero);
		inputs.Add(Float4.One);
		inputs.Add(Float4.Half);
		inputs.Add((Float4)0.0031308d); //Thresholds
		inputs.Add((Float4)0.04045d);
		inputs.Add(new Float4(0f, 0f, 0f, 1f));
		inputs.Add(new Float4(1f, 1f, 1f, 0f));
		inputs.Add(new Float4(0.2f, 0.1f, 0.6f, 0.1234f));

		var random = new SystemPrng(42);

		for (int i = 0; i < 1000; i++) inputs.Add(random.Next4());
	}

	static readonly List<Float4> inputs = new();


	[Test]
	public void LinearToGamma([ValueSource(nameof(inputs))] Float4 input)
	{
		float expectX = LinearToGammaExact(input.X);
		float expectY = LinearToGammaExact(input.Y);
		float expectZ = LinearToGammaExact(input.Z);
		float expectW = input.W;

		Float4 output = SystemSerializer.LinearToGamma(input);

		Assert.That(output >= Float4.Zero);
		Assert.That(output <= Float4.One);

		const float Threshold = 0.015f;
		Assert.That(output.X, FastMath.AlmostZero(expectX) ? Utility.AlmostZero() : Is.EqualTo(expectX).Roughly(Threshold));
		Assert.That(output.Y, FastMath.AlmostZero(expectY) ? Utility.AlmostZero() : Is.EqualTo(expectY).Roughly(Threshold));
		Assert.That(output.Z, FastMath.AlmostZero(expectZ) ? Utility.AlmostZero() : Is.EqualTo(expectZ).Roughly(Threshold));
		Assert.That(output.W, FastMath.AlmostZero(expectW) ? Utility.AlmostZero() : Is.EqualTo(expectW).Roughly(Threshold));
	}

	[Test]
	public void GammaToLinear([ValueSource(nameof(inputs))] Float4 input)
	{
		float expectX = GammaToLinearExact(input.X);
		float expectY = GammaToLinearExact(input.Y);
		float expectZ = GammaToLinearExact(input.Z);
		float expectW = input.W;

		Float4 output = SystemSerializer.GammaToLinear(input);

		Assert.That(output >= Float4.Zero);
		Assert.That(output <= Float4.One);
		
		const float Threshold = 0.004f;
		Assert.That(output.X, FastMath.AlmostZero(expectX) ? Utility.AlmostZero() : Is.EqualTo(expectX).Roughly(Threshold));
		Assert.That(output.Y, FastMath.AlmostZero(expectY) ? Utility.AlmostZero() : Is.EqualTo(expectY).Roughly(Threshold));
		Assert.That(output.Z, FastMath.AlmostZero(expectZ) ? Utility.AlmostZero() : Is.EqualTo(expectZ).Roughly(Threshold));
		Assert.That(output.W, FastMath.AlmostZero(expectW) ? Utility.AlmostZero() : Is.EqualTo(expectW).Roughly(Threshold));
	}

	static float LinearToGammaExact(float value)
	{
		double result;

		if (value <= 0.0031308d) result = value * 12.92d;
		else result = 1.055d * Math.Pow(value, 1d / 2.4f) - 0.055f;

		return (float)result;
	}

	static float GammaToLinearExact(float value)
	{
		double result;

		if (value <= 0.04045d) result = value / 12.92d;
		else result = Math.Pow((value + 0.055d) / 1.055d, 2.4f);

		return (float)result;
	}
}