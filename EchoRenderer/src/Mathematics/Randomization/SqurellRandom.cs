using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EchoRenderer.Mathematics.Randomization;

/// <summary>
/// Hash based pseudorandom number generator based on Squirrel Eiserloh's GDC 2017 talk "Noise-Based RNG"
/// </summary>
public class SquirrelRandom : IRandom
{
	public SquirrelRandom() : this
	(
		(uint)HashCode.Combine
		(
			Thread.CurrentThread,
			Environment.TickCount64,
			Interlocked.Increment(ref globalSeed)
		)
	) { }

	public SquirrelRandom(uint seed)
	{
		this.seed = seed;
		state = seed;
	}

	uint state;
	readonly uint seed;
	static long globalSeed;

	const double Scale = 1d / (uint.MaxValue + 1L);

	/// <inheritdoc/>
	public float Next1() => (float)(Next() * Scale);

	/// <inheritdoc/>
	public int Next1(int max) => Next(max);

	/// <inheritdoc/>
	public int Next1(int min, int max) => Next((long)max - min) + min;

	int Next(long max) => (int)(Next() * Scale * max);

	uint Next()
	{
		Mangle(ref state);
		return state;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void Mangle(ref uint source)
	{
		source *= 0x773598E9u;
		source += seed /*  */;
		source ^= source >> 8;
		source += 0x3B9AEE2Bu;
		source ^= source << 8;
		source *= 0x6B49DCD5u;
		source ^= source >> 8;
	}

	/// <summary>
	/// Returns a randomly hashed and mangled value from <paramref name="source"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Mangle(uint source)
	{
		source *= 0xED7D6509u;
		source ^= source >> 8;
		source += 0x6A5F4471u;
		source ^= source << 8;
		source *= 0x2D96212Bu;
		source ^= source >> 8;

		return source;
	}
}