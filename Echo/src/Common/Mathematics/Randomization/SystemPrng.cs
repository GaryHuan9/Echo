using System;

namespace Echo.Common.Mathematics.Randomization;

public sealed record SystemPrng : Prng
{
	public SystemPrng(uint? seed = null) => random = seed == null ? new Random() : new Random((int)seed.Value);

	readonly Random random;

	public override float Next1() => random.NextSingle();

	public override int Next1(int max) => random.Next(max);

	public override int Next1(int min, int max) => random.Next(min, max);

	public bool Equals(SquirrelPrng other) => base.Equals(other);
	public override int GetHashCode() => base.GetHashCode();
}