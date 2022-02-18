using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions;

/// <summary>
/// A sample on an one dimensional distribution between zero (inclusive) and one (exclusive)
/// </summary>
public readonly struct Distro1
{
	Distro1(float u) => this.u = FastMath.ClampEpsilon(u);

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
		return (int)FastMath.FMA(u, max - min, min);
	}

	/// <summary>
	/// Extracts and outputs <paramref name="index"/> which is between zero (inclusive) and <paramref name="range"/> (exclusive).
	/// Returns a new uniform and unbiased <see cref="Distro1"/> that is a more 'zoomed in' version of this <see cref="Distro1"/>.
	/// </summary>
	public Distro1 Extract(int range, out int index)
	{
		index = Range(range);
		return (Distro1)FastMath.FMA(u, range, -index);
	}

	public static implicit operator float(Distro1 distro) => distro.u;
	public static explicit operator Distro1(float value) => new(value);
}