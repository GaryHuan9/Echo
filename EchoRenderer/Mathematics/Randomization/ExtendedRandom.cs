using System;

namespace EchoRenderer.Mathematics.Randomization
{
	public class ExtendedRandom : Random, IRandom
	{
		public ExtendedRandom(int seed) : base(seed) { }

		public float Next() => (float)NextDouble();
	}
}