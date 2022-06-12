using System;
using System.Numerics;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;

namespace Echo.Core.Common.Mathematics.Randomization;

/// <summary>
/// A pseudorandom number generator.
/// </summary>
public abstract record Prng
{
	/// <summary>
	/// Returns a thread-safe <see cref="uint"/> value that is globally random.
	/// </summary>
	protected static uint RandomValue
	{
		get
		{
			Span<byte> bytes = stackalloc byte[sizeof(uint)];

			Random.Shared.NextBytes(bytes);
			return BitConverter.ToUInt32(bytes);
		}
	}

	/// <summary>
	/// Returns the next uniform pseudorandom <see cref="uint"/> value.
	/// </summary>
	/// <returns>The <see cref="uint"/> value, from <see cref="uint.MinValue"/>
	/// (inclusive) to <see cref="uint.MaxValue"/> (inclusive).</returns>
	public abstract uint NextUInt32();

	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public float Next1()
	{
		//Implemented based on blog from Marc B. Reynolds:
		//https://marc-b-reynolds.github.io/distribution/2017/01/17/DenseFloat.html#the-parts-im-not-tell-you

		ulong source = ((ulong)NextUInt32() << 32) | NextUInt32();
		uint count = (uint)BitOperations.LeadingZeroCount(source);

		if (count <= 40)
		{
			const uint Mask = (1u << 23) - 1u;

			uint exponent = (126u - count) << 23;
			uint mantissa = (uint)source & Mask;

			return Scalars.UInt32ToSingleBits(exponent | mantissa);
		}

		//The following code will only be reached 1 out of 2^40 times (probabilistically).
		//The magic multiplier equals IEEE float with mantissa = zero and exponent = 0b111111.

		return (uint)source * 5.421011e-20f;
	}

	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public float Next1(float max) => Next1() * max;

	/// <summary>
	/// Returns the next pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public float Next1(float min, float max)
	{
		float distance = max - min;
		return FastMath.FMA(distance, Next1(), min);
	}

	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Next1(int max)
	{
		Assert.IsTrue(max > 0);
		return (int)Next1Impl((uint)max);
	}

	/// <summary>
	/// Returns the next pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Next1(int min, int max)
	{
		Assert.IsTrue(max > min);
		uint distance = (uint)((long)max - min);
		return (int)Next1Impl(distance) + min;
	}

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public Float2 Next2() => new(Next1(), Next1());

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float2 Next2(float max) => new(Next1(max), Next1(max));

	/// <summary>
	/// Returns the next two pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float2 Next2(float min, float max) => new(Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Next2(int max) => new(Next1(max), Next1(max));

	/// <summary>
	/// Returns the next two pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Next2(int min, int max) => new(Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public Float3 Next3() => new(Next1(), Next1(), Next1());

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float3 Next3(float max) => new(Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next three pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float3 Next3(float min, float max) => new(Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int3 Next3(int max) => new(Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next three pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int3 Next3(int min, int max) => new(Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public Float4 Next4() => new(Next1(), Next1(), Next1(), Next1());

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float4 Next4(float max) => new(Next1(max), Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next four pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Float4 Next4(float min, float max) => new(Next1(min, max), Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int4 Next4(int max) => new(Next1(max), Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next four pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int4 Next4(int min, int max) => new(Next1(min, max), Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns a random vector inside a unit sphere.
	/// </summary>
	public Float3 NextInSphere()
	{
		Float3 random;

		do random = Next3(-1f, 1f);
		while (random.SquaredMagnitude > 1f);

		return random;
	}

	/// <summary>
	/// Returns a random vector inside a sphere with <paramref name="radius"/>.
	/// </summary>
	public Float3 NextInSphere(float radius) => NextInSphere() * radius;

	/// <summary>
	/// Returns a random unit vector that is on a unit sphere.
	/// </summary>
	public Float3 NextOnSphere() => NextInSphere().Normalized;

	/// <summary>
	/// Returns a random vector that is on a sphere with <paramref name="radius"/>.
	/// </summary>
	public Float3 NextOnSphere(float radius) => NextInSphere().Normalized * radius;

	/// <summary>
	/// Returns a random value on the gaussian distribution curve. Implementation based
	/// on the Box-Muller transform with a standard deviation of 1 and mean of 0.
	/// </summary>
	public float NextGaussian()
	{
		float u0 = 1f - Next1();
		float u1 = 1f - Next1();

		return FastMath.Sqrt0(-2f * MathF.Log(u0)) * MathF.Sin(Scalars.Tau * u1);
	}

	/// <summary>
	/// Returns a randomly gaussian distributed point with mean
	/// at (0.5, 0.5) and clamped between (0, 0) and (1, 1).
	/// </summary>
	public Float2 NextSample()
	{
		Float2 position = new Float2(NextGaussian(), NextGaussian()) / 6f;
		return position.Clamp(Float2.NegativeHalf, Float2.Half) + Float2.Half;
	}

	/// <summary>
	/// Completely and uniformly shuffles the content stored in <paramref name="span"/>.
	/// </summary>
	public void Shuffle<T>(Span<T> span)
	{
		for (int i = span.Length - 1; i > 0; i--) CodeHelper.Swap(ref span[i], ref span[Next1(i + 1)]);
	}

	public virtual bool Equals(Prng other) => other?.GetType() == GetType();
	public override int GetHashCode() => GetType().GetHashCode();
  
	/// <summary>
	/// Computes a discrete uniform random value from 0 (inclusive) to <paramref name="max"/> (exclusive).
	/// Implementation based on academic paper by Daniel Lemire: https://arxiv.org/abs/1805.10941
	/// </summary>
	uint Next1Impl(uint max)
	{
		Assert.AreNotEqual(max, 0u);

		ulong value = (ulong)NextUInt32() * max;
		if ((uint)value >= max) goto exit;

		uint threshold = (uint)-max - (uint)-max / max * max;
		while ((uint)value < threshold) value = (ulong)NextUInt32() * max;

	exit:
		return (uint)(value >> 32);
	}
}