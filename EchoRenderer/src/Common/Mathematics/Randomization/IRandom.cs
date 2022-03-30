using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;

namespace EchoRenderer.Common.Mathematics.Randomization;

public interface IRandom
{
	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	float Next1();

	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed float Next1(float max) => Next1() * max;

	/// <summary>
	/// Returns the next pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed float Next1(float min, float max)
	{
		float distance = max - min;
		return FastMath.FMA(distance, Next1(), min);
	}

	/// <summary>
	/// Returns the next pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Next1(int max) => (int)Next1((float)max);

	/// <summary>
	/// Returns the next pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Next1(int min, int max)
	{
		long distance = (long)max - min;

		if (distance <= int.MaxValue) return Next1((int)distance) + min;
		throw new Exception($"Cannot fetch value with range {distance}");

		//We must manually override this in implementations to support larger ranges
	}

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public sealed Float2 Next2() => new(Next1(), Next1());

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float2 Next2(float max) => new(Next1(max), Next1(max));

	/// <summary>
	/// Returns the next two pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float2 Next2(float min, float max) => new(Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next two pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int2 Next2(int max) => new(Next1(max), Next1(max));

	/// <summary>
	/// Returns the next two pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int2 Next2(int min, int max) => new(Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public sealed Float3 Next3() => new(Next1(), Next1(), Next1());

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float3 Next3(float max) => new(Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next three pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float3 Next3(float min, float max) => new(Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next three pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int3 Next3(int max) => new(Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next three pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int3 Next3(int min, int max) => new(Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and one (exclusive).
	/// </summary>
	public sealed Float4 Next4() => new(Next1(), Next1(), Next1(), Next1());

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float4 Next4(float max) => new(Next1(max), Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next four pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Float4 Next4(float min, float max) => new(Next1(min, max), Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns the next four pseudorandom number between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int4 Next4(int max) => new(Next1(max), Next1(max), Next1(max), Next1(max));

	/// <summary>
	/// Returns the next four pseudorandom number between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public sealed Int4 Next4(int min, int max) => new(Next1(min, max), Next1(min, max), Next1(min, max), Next1(min, max));

	/// <summary>
	/// Returns a random vector inside a unit sphere.
	/// </summary>
	public sealed Float3 NextInSphere()
	{
		Float3 random;

		do random = Next3(-1f, 1f);
		while (random.SquaredMagnitude > 1f);

		return random;
	}

	/// <summary>
	/// Returns a random vector inside a sphere with <paramref name="radius"/>.
	/// </summary>
	public sealed Float3 NextInSphere(float radius) => NextInSphere() * radius;

	/// <summary>
	/// Returns a random unit vector that is on a unit sphere.
	/// </summary>
	public sealed Float3 NextOnSphere() => NextInSphere().Normalized;

	/// <summary>
	/// Returns a random vector that is on a sphere with <paramref name="radius"/>.
	/// </summary>
	public sealed Float3 NextOnSphere(float radius) => NextInSphere().Normalized * radius;

	/// <summary>
	/// Returns a random value on the gaussian distribution curve. Implementation based
	/// on the Box-Muller transform with a standard deviation of 1 and mean of 0.
	/// </summary>
	public sealed float NextGaussian()
	{
		float u0 = 1f - Next1();
		float u1 = 1f - Next1();

		return FastMath.Sqrt0(-2f * MathF.Log(u0)) * MathF.Sin(Scalars.TAU * u1);
	}

	/// <summary>
	/// Returns a randomly gaussian distributed point with mean
	/// at (0.5, 0.5) and clamped between (0, 0) and (1, 1).
	/// </summary>
	public sealed Float2 NextSample()
	{
		Float2 position = new Float2(NextGaussian(), NextGaussian()) / 6f;
		return position.Clamp(Float2.NegativeHalf, Float2.Half) + Float2.Half;
	}

	/// <summary>
	/// Completely and uniformly shuffles the content stored in <paramref name="span"/>.
	/// </summary>
	public sealed void Shuffle<T>(Span<T> span)
	{
		for (int i = span.Length - 1; i > 0; i--) CodeHelper.Swap(ref span[i], ref span[Next1(i + 1)]);
	}

	/// <summary>
	/// Returns either a thread-safe random seed or the indicated seed from the nullable <paramref name="seed"/>.
	/// </summary>
	public static uint GetSeed(uint? seed)
	{
		if (seed != null) return seed.Value;

		Span<byte> bytes = stackalloc byte[sizeof(uint)];

		Random.Shared.NextBytes(bytes);
		return BitConverter.ToUInt32(bytes);
	}
}