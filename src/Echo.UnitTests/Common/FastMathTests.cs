﻿using System;
using System.Numerics;
using Echo.Core.Common.Mathematics;
using NUnit.Framework;

namespace Echo.UnitTests.Common;

[TestFixture]
public class FastMathTests
{
	static readonly float[] floatValues =
	{
		0f, -0f, 1f, -1f, 2f, -3f, 11f, -16f, 101f, 3E5f, -7E4f, 0.6f, 1E-8f, 7f, -0.5f, -1E-8f, Scalars.Phi, float.NaN,
		float.Epsilon, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue, float.MinValue, FastMath.Epsilon
	};

	[Test]
	public void OneMinusEpsilon()
	{
		float expected = BitConverter.UInt32BitsToSingle(BitConverter.SingleToUInt32Bits(1f) - 1);
		Assert.That(FastMath.OneMinusEpsilon, Is.EqualTo(expected));
	}

	[Test]
	public void Max0([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value < 0f ? 0f : value;
		Assert.That(FastMath.Max0(value), Is.EqualTo(expected));
	}

	[Test]
	public void Clamp01([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value < 0f ? 0f : value > 1f ? 1f : value;
		Assert.That(FastMath.Clamp01(value), Is.EqualTo(expected));
	}

	[Test]
	public void Clamp11([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value < -1f ? -1f : value > 1f ? 1f : value;
		Assert.That(FastMath.Clamp11(value), Is.EqualTo(expected));
	}

	[Test]
	public void ClampEpsilon([ValueSource(nameof(floatValues))] float value)
	{
		float epsilon = BitConverter.UInt32BitsToSingle(BitConverter.SingleToUInt32Bits(1f) - 1u);
		float expected = value < 0f ? 0f : value >= 1f ? epsilon : value;
		Assert.That(FastMath.ClampEpsilon(value), Is.EqualTo(expected));
	}

	[Test]
	public void Abs([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value < 0f ? -value : value;
		Assert.That(FastMath.Abs(value), Is.EqualTo(expected));
	}

	[Test]
	public void Sqrt0([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value <= 0f ? 0f : (float)Math.Sqrt(value);
		Assert.That(FastMath.Sqrt0(value), Is.EqualTo(expected).Roughly());
	}

	[Test]
	public void SqrtR0([ValueSource(nameof(floatValues))] float value)
	{
		float expected = value <= 0f ? float.PositiveInfinity : (float)(1d / Math.Sqrt(value));
		Assert.That(FastMath.SqrtR0(value), Is.EqualTo(expected).Roughly());
	}

	[Test]
	public void OneMinus2([ValueSource(nameof(floatValues))] float value)
	{
		float expected = (float)(1d - (double)value * value);
		Assert.That(FastMath.OneMinus2(value), Is.EqualTo(expected).Roughly());
	}

	[Test]
	public void Identity([ValueSource(nameof(floatValues))] float value)
	{
		float identity = FastMath.Identity(value);

		double expected0 = Math.Sin(Math.Acos(value));
		double expected1 = Math.Cos(Math.Asin(value));
		float expected = (float)((expected0 + expected1) / 2d);

		if (value is <= -1f or >= 1f) Assert.That(identity, Is.Zero);
		else Assert.That(identity, Is.EqualTo(expected).Roughly());
	}

	[Test]
	public void FMA([ValueSource(nameof(floatValues))] float value,
					[ValueSource(nameof(floatValues))] float multiplier)
	{
		float adder = MathF.Sin(BitOperations.RotateRight((uint)value.GetHashCode(), 7) ^ multiplier.GetHashCode()); //Basically just a random number
		float expected = (float)((double)value * multiplier + adder);
		Assert.That(FastMath.FMA(value, multiplier, adder), Is.EqualTo(expected).Roughly());
	}

	[Test]
	public void SinCos([ValueSource(nameof(floatValues))] float value)
	{
		FastMath.SinCos(value, out float sin, out float cos);
		Assert.That(sin, Is.EqualTo((float)Math.Sin(value)).Roughly(0.01f));
		Assert.That(cos, Is.EqualTo((float)Math.Cos(value)).Roughly(0.01f));
	}

	[Test]
	public void Positive([ValueSource(nameof(floatValues))] float value)
	{
		Assert.That(FastMath.Positive(value), Is.EqualTo(FastMath.Epsilon <= value));
	}

	[Test]
	public void AlmostZero([ValueSource(nameof(floatValues))] float value)
	{
		Assert.That(FastMath.AlmostZero(value), Is.EqualTo(value is > -FastMath.Epsilon and < FastMath.Epsilon));
	}
}