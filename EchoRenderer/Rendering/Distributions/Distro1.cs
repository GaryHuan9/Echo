using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Distributions
{
	/// <summary>
	/// A sample on an one dimensional distribution between zero (inclusive) and one (exclusive)
	/// </summary>
	public readonly struct Distro1
	{
		public Distro1(float u) => this.u = FastMath.ClampEpsilon(u);

		public readonly float u;

		/// <summary>
		/// Maps this <see cref="Distro1"/> to be between zero (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public int Range(int max)
		{
			Assert.IsTrue(max > 0);
			return (int)(u * max);
		}

		/// <summary>
		/// Maps this <see cref="Distro1"/> to be between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
		/// </summary>
		public int Range(int min, int max)
		{
			Assert.IsTrue(min < max);
			return Range(max - min) + min;
		}

		public static implicit operator float(Distro1 distro) => distro.u;
	}
}