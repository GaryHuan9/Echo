using System;

namespace EchoRenderer.Common.Mathematics.Randomization;

public class SystemRandom : Random, IRandom
{
	public SystemRandom(uint seed) : base((int)seed) { }

	/// <inheritdoc/>
	public float Next1() => (float)NextDouble();

	/// <inheritdoc/>
	public int Next1(int max) => Next(max);

	/// <inheritdoc/>
	public int Next1(int min, int max) => Next(min, max);
}