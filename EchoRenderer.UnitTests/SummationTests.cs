using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class SummationTests
{
	[Test]
	public void Constructor()
	{
		var vector = Vector128.Create(1f, 0f, -1f, 100f);
		Summation sum = new Summation(vector);
		Assert.That(sum.Result, Is.EqualTo(vector));
	}

	[Test]
	public void Zero()
	{
		Assert.That(Summation.Zero.Result, Is.EqualTo(Vector128<float>.Zero));
	}

	[Test]
	public void Positives()
	{
		const int Length = 1000000;

		var sum = new Summation(Vector128.Create((float)Length));
		long truth = Length;

		for (int i = Length - 1; i >= 0; i--)
		{
			sum += Vector128.Create((float)i);
			truth += i;
		}

		var error = Sse.Subtract(sum.Result, Vector128.Create((float)truth));

		Assert.That(PackedMath.AlmostZero(error));
	}

	[Test]
	[Repeat(100)]
	public void Randoms()
	{
		var random = Utilities.NewRandom();
		Summation sum = Summation.Zero;
		decimal truth = 0m;

		for (int i = 0; i < 100000; i++)
		{
			float value = NextBiased(random);
			sum += Vector128.Create(value);
			truth += (decimal)value;
		}

		Assert.That(truth, Is.EqualTo(sum.Result.GetElement(0)).Roughly());
	}

	[Test]
	[Repeat(10)]
	public void SeriesRandom()
	{
		const int Length = 1000;

		var random = Utilities.NewRandom();
		Summation total = Summation.Zero;
		decimal truth = 0m;

		for (int i = 0; i < Length; i++)
		{
			Summation sum = Summation.Zero;

			for (int j = 0; j < Length; j++)
			{
				float value = NextBiased(random);
				sum += Vector128.Create(value);
				truth += (decimal)value;
			}

			total += sum;
		}

		Assert.That(truth, Is.EqualTo(total.Result.GetElement(0)).Roughly());
	}

	static float NextBiased(IRandom random) => random.Next1() < 0.5f ? -random.Next1() : random.Next1() * 8f;
}