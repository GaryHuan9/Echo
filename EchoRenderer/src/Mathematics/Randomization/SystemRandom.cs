using System;

namespace EchoRenderer.Mathematics.Randomization
{
	public class SystemRandom : Random, IRandom
	{
		public SystemRandom(uint seed) : base((int)seed) { }

		public float Next1() => (float)NextDouble();

		public int Next1(int max) => Next(max);

		public int Next1(int min, int max) => Next(min, max);
	}
}