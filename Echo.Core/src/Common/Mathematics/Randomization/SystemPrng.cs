using System;

namespace Echo.Core.Common.Mathematics.Randomization;

public sealed record SystemPrng : Prng
{
	public SystemPrng(uint? seed = null)
	{
		random = CreateRandom(seed);
		this.seed = seed;
	}

	SystemPrng(Random random) => this.random = random;

	SystemPrng(SystemPrng source) : base(source)
	{
		random = CreateRandom(source.seed);
		seed = source.seed;
	}

	readonly Random random;
	readonly uint? seed;

	public static SystemPrng Shared { get; } = new(Random.Shared);

	public override uint NextUInt32() => (uint)(random.NextInt64() >> 1);

	static Random CreateRandom(uint? seed) => seed == null ? new Random() : new Random((int)seed.Value);
}