using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Evaluation.Distributions;

/// <summary>
/// A sample on an one dimensional distribution between zero (inclusive) and one (exclusive)
/// </summary>
public readonly struct Sample1D
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Sample1D(float u)
	{
		Assert.IsFalse(float.IsNaN(u));
		this.u = FastMath.ClampEpsilon(u);
	}

	readonly float u;

	/// <summary>
	/// Maps this <see cref="Sample1D"/> to an integer.
	/// </summary>
	/// <param name="max">The upper bound of this mapping (exclusive).</param>
	/// <returns>The mapped integer between zero (inclusive) and <paramref name="max"/>.</returns>
	public int Range(int max)
	{
		Assert.IsTrue(max > 0);
		return (int)(u * max);
	}

	/// <summary>
	/// Maps this <see cref="Sample1D"/> to an integer.
	/// </summary>
	/// <param name="min">The lower bound of this mapping (inclusive).</param>
	/// <param name="max">The upper bound of this mapping (exclusive).</param>
	/// <returns>The mapped integer between <paramref name="min"/> and <paramref name="max"/>.</returns>
	public int Range(int min, int max)
	{
		Assert.IsTrue(min < max);
		return (int)FastMath.FMA(u, max - min, min);
	}

	/// <summary>
	/// Maps this <see cref="Sample1D"/> to an integer.
	/// </summary>
	/// <param name="max">The upper bound of this mapping (exclusive).</param>
	/// <param name="index">Outputs the mapped integer between zero (inclusive) and <paramref name="max"/>.</param>
	/// <returns>A stretched version of this <see cref="Sample1D"/> that is readjusted to be uniform and unbiased.
	/// Invoking this method is similar to invoking <see cref="Range(int)"/> and then using <see cref="Stretch"/>
	/// to readjust the original <see cref="Sample1D"/>.</returns>
	public Sample1D Range(int max, out int index)
	{
		index = Range(max);
		return (Sample1D)FastMath.FMA(u, max, -index);
	}

	/// <summary>
	/// Stretches this <see cref="Sample1D"/> to a more 'zoomed in' version.
	/// </summary>
	/// <param name="lower">The lower bound of this stretch; the value of this <see cref="Sample1D"/> should
	/// be greater than or equals to this parameter.</param>
	/// <param name="upper">The upper bound of this stretch; the value of this <see cref="Sample1D"/> should
	/// be lesser than this parameter.</param>
	/// <returns>The new uniform <see cref="Sample1D"/> that is created from this stretch operation.</returns>
	/// <remarks>If a <see cref="Sample1D"/> is used during a sampling, then its value should be considered
	/// compromised and subsequent sampling on the same <see cref="Sample1D"/> will be biased. Nevertheless,
	/// we can still reuse a <see cref="Sample1D"/> (to a certain extend) if it was used to sample a discrete
	/// value, by 'stretching' the original <see cref="Sample1D"/> to create a new <see cref="Sample1D"/> that
	/// is uniform and unbiased. To do so, this <see cref="Stretch"/> method requires the two bounds, within
	/// which categorized the previously sampled discrete value, to produce a new <see cref="Sample1D"/> by
	/// utilizing the continuous (fractional) part of the discrete quantization.</remarks>
	public Sample1D Stretch(float lower, float upper)
	{
		Assert.IsTrue(u >= lower);
		Assert.IsTrue(u < upper);

		return (Sample1D)((u - lower) / (upper - lower));
	}

	public static implicit operator float(Sample1D sample) => sample.u;
	public static explicit operator Sample1D(float value) => new(value);
}