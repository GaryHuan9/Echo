using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Mathematics
{
	public class HashRandom
	{
		public HashRandom(double seed) => this.seed = seed;

		double seed;

		public float NextFloat()
		{
			double value = Math.Sin(seed * 12.9898d);
			seed = (seed + Math.E).Repeat(1E4d);
			return (float)(value * 43758.545312d).Repeat(1d);
		}
	}
}