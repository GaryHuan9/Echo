using System;

namespace Echo.Common.Mathematics.Randomization;

public sealed record SystemPrng : Prng
{
	public SystemPrng(uint? seed = null) => random = seed == null ? new Random() : new Random((int)seed.Value);

	readonly Random random;

	public override uint NextUInt32() => (uint)(random.NextInt64() >> 1);
}