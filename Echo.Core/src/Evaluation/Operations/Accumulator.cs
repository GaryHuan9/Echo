using System;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics.Primitives;

namespace Echo.Core.Evaluation.Operations;

/// <summary>
/// Mutable struct that stores the accumulating evaluated samples of one pixel.
/// </summary>
public struct Accumulator
{
	Summation average;
	Summation squared;

	uint count;

	/// <summary>
	/// The average of all of the samples.
	/// </summary>
	public readonly Float4 Value => average.Result;

	/// <summary>
	/// The unbiased sample variance of all of the samples using Welford's online algorithm.
	/// </summary>
	public readonly Float4 Variance => count < 3 ? squared.Result : squared.Result / (count - 1);

	/// <summary>
	/// The remaining noise of the samples, which is their <see cref="Variance"/>
	/// divided by the square root of their population (the number of samples).
	/// </summary>
	public readonly Float4 Noise => squared.Result * MathF.ReciprocalSqrtEstimate(Math.Max(1, count * count * count));

	/// <summary>
	/// Adds a <paramref name="sample"/> to this <see cref="Accumulator"/>.
	/// </summary>
	/// <param name="sample">The new input sample to add to this pixel.</param>
	/// <returns>False if the input was rejected because it was invalid, true otherwise.</returns>
	public bool Add(in Float4 sample)
	{
		if (!float.IsFinite(sample.Sum)) return false; //Gates degenerate values

		++count;

		Summation delta = average - sample;
		average -= delta / (Float4)count;
		squared += delta * (average - sample).Result;

		return true;
	}
}