using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using NUnit.Framework;

namespace Echo.UnitTests.Common;

[TestFixture]
[Parallelizable]
public class CurvesTests
{
	static readonly uint normalStart = BitConverter.SingleToUInt32Bits(0f);
	static readonly uint normalEnd = BitConverter.SingleToUInt32Bits(1f);

	static readonly Func<float, float>[] curves = { Curves.Sigmoid, Curves.EaseIn, Curves.EaseOut, Curves.EaseInSmooth, Curves.EaseOutSmooth };
	static readonly float[] outputRangeInput = { 0f, 0.4f, 1f, float.Epsilon, FastMath.OneMinusEpsilon, float.NegativeInfinity, float.PositiveInfinity };

	[Test]
	public void NeverDecreasing([ValueSource(nameof(curves))] Func<float, float> curve)
	{
		float lastOutput = 0f;

		for (uint i = normalStart; i <= normalEnd; i++)
		{
			float input = BitConverter.UInt32BitsToSingle(i);
			float output = curve(input);

			if (output < lastOutput)
			{
				TestContext.WriteLine(i);
				TestContext.WriteLine(input.ToString("F10"));
				Assert.GreaterOrEqual(output, lastOutput);
			}
			
			lastOutput = output;
		}

		Assert.AreEqual(lastOutput, 1f);
	}

	[Test]
	public void OutputRange([ValueSource(nameof(curves))] Func<float, float> curve,
							[ValueSource(nameof(outputRangeInput))] float input)
	{
		float output = curve(input);

		if (input <= 0f) Assert.AreEqual(output, 0f);
		else if (input >= 1f) Assert.AreEqual(output, 1f);
		else Assert.That(output, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f));
	}
}