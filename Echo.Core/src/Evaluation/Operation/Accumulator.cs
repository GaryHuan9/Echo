using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics.Primitives;

namespace Echo.Core.Evaluation.Operation;

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
	/// The remaining noise of the samples, which is the square root of their <see cref="Variance"/>
	/// divided by their population (the number of samples) divided by their <see cref="Value"/>.
	/// </summary>
	public readonly Float4 Noise
	{
		get
		{
			if (count < 2) return Float4.Zero;

			//Some algebra allows us to simply the equation:
			//Sqrt(Variance) / (count - 1) / Value = 1 / Sqrt((count - 1)^3 * Value^2 / squared.Result)
			//Then at the end we can use a binary mask to remove the degenerate case when Value is zero

			uint oneLess = count - 1;
			oneLess *= oneLess * oneLess;

			Vector128<float> numerator = (Value * Value * oneLess).v;
			Vector128<float> denominator = Sse.Reciprocal(squared.Result.v);

			Vector128<float> notZero = Sse.CompareNotEqual(numerator, Vector128<float>.Zero);
			Vector128<float> result = Sse.ReciprocalSqrt(Sse.Multiply(numerator, denominator));

			return new Float4(Sse.And(notZero, result));
		}
	}

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