using System.Runtime.CompilerServices;

namespace Echo.Core.Common.Mathematics.Randomization;

/// <summary>
/// Hash based pseudorandom number generator based on
/// Squirrel Eiserloh's GDC 2017 talk "Noise-Based RNG".
/// </summary>
public sealed record SquirrelPrng : Prng
{
	public SquirrelPrng(uint? seed = null)
	{
		this.seed = seed ?? RandomValue;
		state = 1;
	}

	SquirrelPrng(SquirrelPrng source) : base(source)
	{
		seed = RandomValue;
		state = 1;
	}

	readonly uint seed;
	uint state;

	public override uint NextUInt32()
	{
		Mangle(ref state);
		return state;
	}

	public bool Equals(SquirrelPrng other) => base.Equals(other);
	public override int GetHashCode() => base.GetHashCode();

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