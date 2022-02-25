using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions;

/// <summary>
/// A sample on an one dimensional distribution between zero (inclusive) and one (exclusive)
/// </summary>
public readonly struct Sample1D
{
	Sample1D(float u)
	{
		Assert.IsFalse(float.IsNaN(u));
		this.u = FastMath.ClampEpsilon(u);
	}

	public readonly float u;

	/// <summary>
	/// Maps this <see cref="Sample1D"/> to be between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Range(int max)
	{
		Assert.IsTrue(max > 0);
		return (int)(u * max);
	}

	/// <summary>
	/// Maps this <see cref="Sample1D"/> to be between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public int Range(int min, int max)
	{
		Assert.IsTrue(min < max);
		return (int)FastMath.FMA(u, max - min, min);
	}

	/// <summary>
	/// Extracts and outputs <paramref name="index"/> which is between zero (inclusive) and <paramref name="range"/> (exclusive).
	/// Returns a new uniform and unbiased <see cref="Sample1D"/> that is a more 'zoomed in' version of this <see cref="Sample1D"/>.
	/// </summary>
	public Sample1D Extract(int range, out int index)
	{
		index = Range(range);
		return (Sample1D)FastMath.FMA(u, range, -index);
	}

	/// <summary>
	/// When this <see cref="Sample1D"/> is between <paramref name="lower"/> (inclusive) and <paramref name="upper"/> (exclusive),
	/// returns a new <see cref="Sample1D"/> that is this <see cref="Sample1D"/> stretched, with the two bounds to zero and one.
	/// </summary>
	public Sample1D Zoom(float lower, float upper)
	{
		Assert.IsTrue(u >= lower);
		Assert.IsTrue(u < upper);

		float domainR = 1f / (upper - lower);
		return (Sample1D)((u - lower) * domainR);
	}

	public static implicit operator float(Sample1D sample) => sample.u;
	public static explicit operator Sample1D(float value) => new(value);
}