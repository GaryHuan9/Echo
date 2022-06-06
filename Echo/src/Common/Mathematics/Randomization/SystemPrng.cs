using System;

namespace Echo.Common.Mathematics.Randomization;

public sealed record SystemPrng : Prng
{
	public SystemPrng(uint? seed = null) : this(seed == null ? new Random() : new Random((int)seed.Value)) { }

	SystemPrng(Random random) => this.random = random;

	readonly Random random;

	public static SystemPrng Shared { get; } = new(Random.Shared);

	public override uint NextUInt32() => (uint)(random.NextInt64() >> 1);

	public bool Equals(SquirrelPrng other) => base.Equals(other);
	public override int GetHashCode() => base.GetHashCode();
}