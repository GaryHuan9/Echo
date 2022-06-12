using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Mathematics.Randomization;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixture]
public class SummationTests
{
	[Test]
	public void Constructor()
	{
		var float4 = new Float4(1f, 0f, -1f, 100f);
		var sum = new Summation(float4);
		Assert.That(sum.Result, Is.EqualTo(float4));
	}

	[Test]
	public void Zero()
	{
		Assert.That(Summation.Zero.Result, Is.EqualTo(Float4.Zero));
	}

	[Test]
	public void Positives()
	{
		const int Length = 1000000;

		var sum = new Summation((Float4)Length);
		long truth = Length;

		for (int i = Length - 1; i >= 0; i--)
		{
			sum += (Float4)i;
			truth += i;
		}

		Float4 error = sum.Result - (Float4)truth;
		Assert.That(error, Is.EqualTo(Float4.Zero));
	}

	[Test]
	[Repeat(100)]
	public void Randoms()
	{
		var random = Utility.NewRandom();
		Summation sum = Summation.Zero;
		decimal truth = 0m;

		for (int i = 0; i < 100000; i++)
		{
			float value = NextBiased(random);
			sum += (Float4)value;
			truth += (decimal)value;
		}

		Assert.That(truth, Is.EqualTo(sum.Result.X).Roughly());
	}

	[Test]
	[Repeat(10)]
	public void SeriesRandom()
	{
		const int Length = 1000;

		var random = Utility.NewRandom();
		Summation total = Summation.Zero;
		decimal truth = 0m;

		for (int i = 0; i < Length; i++)
		{
			Summation sum = Summation.Zero;

			for (int j = 0; j < Length; j++)
			{
				float value = NextBiased(random);
				sum += (Float4)value;
				truth += (decimal)value;
			}

			total += sum;
		}

		Assert.That(truth, Is.EqualTo(total.Result.X).Roughly());
	}

	static float NextBiased(Prng random) => random.Next1() < 0.5f ? -random.Next1() : random.Next1() * 8f;
}