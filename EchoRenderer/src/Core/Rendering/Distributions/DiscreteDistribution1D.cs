using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions;

/// <summary>
/// A one dimensional piecewise distribution constructed from a function of discrete probability destiny values.
/// </summary>
public class DiscreteDistribution1D
{
	public DiscreteDistribution1D(ReadOnlySpan<float> pdfValues)
	{
		Assert.IsFalse(pdfValues.IsEmpty);

		int length = pdfValues.Length;
		cdfValues = new float[length];
		countR = 1f / length;

		//Find the total sum and initialize cdf
		double rolling = 0d;

		for (int i = 0; i < length; i++)
		{
			Assert.IsFalse(pdfValues[i] < 0f); //PDF should not be negative
			cdfValues[i] = (float)(rolling += pdfValues[i]);
		}

		sum = (float)rolling;

		//Normalize the cdf
		if (FastMath.AlmostZero(sum))
		{
			//If the total sum is zero, it means our function has a constant probability of zero, which is
			//technically not a correct function, so we will handle it like a non-zero constant function.

			for (int i = 0; i < length; i++) cdfValues[i] = FastMath.FMA(i, countR, countR);

			sum = 0f; //Sum is still zero though
		}
		else
		{
			//Normalizes the cdf by dividing by the total integral
			float sumR = 1f / sum;

			for (int i = 0; i < length; i++) cdfValues[i] *= sumR;
		}

		integral = sum * countR;

		//Assign the last identical values to one to ensure no leaking when sampling
		int index = length - 1;
		float last = cdfValues[index];

		do cdfValues[index] = 1f;
		while (--index > 0 && last.Equals(cdfValues[index]));
	}

	/// <summary>
	/// The total sum of the input probability density function.
	/// </summary>
	public readonly float sum;

	/// <summary>
	/// The integral across the input probability density function.
	/// </summary>
	public readonly float integral;

	/// <summary>
	/// Cumulative density function values.
	/// </summary>
	readonly float[] cdfValues;

	/// <summary>
	/// The reciprocal of <see cref="Count"/>.
	/// </summary>
	readonly float countR;

	/// <summary>
	/// The total number of discrete values in this <see cref="DiscreteDistribution1D"/>.
	/// </summary>
	public int Count => cdfValues.Length;

	/// <summary>
	/// Samples this <see cref="DiscreteDistribution1D"/> at continuous linear intervals
	/// based on <paramref name="sample"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public Sample1D Sample(Sample1D sample, out float pdf)
	{
		//Find index and lower and upper bounds
		int index = FindIndex(sample);
		GetBounds(index, out float lower, out float upper);

		//Export values
		pdf = (upper - lower) * Count;
		Assert.AreNotEqual(pdf, 0f);

		float shift = Scalars.InverseLerp(lower, upper, sample);
		return (Sample1D)((shift + index) * countR);
	}

	/// <summary>
	/// Finds a discrete point from this <see cref="DiscreteDistribution1D"/> based on
	/// <paramref name="sample"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public int Find(Sample1D sample, out float pdf)
	{
		int index = FindIndex(sample);
		GetBounds(index, out float lower, out float upper);

		pdf = upper - lower;
		return index;
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="DiscreteDistribution1D"/>
	/// if we sampled <paramref name="sample"/> from <see cref="Sample"/>.
	/// </summary>
	public float ProbabilityDensity(Sample1D sample)
	{
		GetBounds(sample.Range(Count), out float lower, out float upper);
		return (upper - lower) * Count;
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="DiscreteDistribution1D"/>
	/// if we found <paramref name="point"/> from <see cref="Find"/>.
	/// </summary>
	public float ProbabilityDensity(int point)
	{
		GetBounds(point, out float lower, out float upper);
		return upper - lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int FindIndex(Sample1D sample)
	{
		int index = new ReadOnlySpan<float>(cdfValues).BinarySearch(sample.u);

		//Majority of the times we will simply exit because we are between two anchors
		Assert.IsTrue(~index < Count);
		if (index < 0) return ~index;

		//When we landed exactly on an anchor, we need to perform some special checks
		GetBounds(index, out float lower, out float upper);

		float pdf = upper - lower;
		if (pdf > 0f) return index;

		//If our pdf is zero, then it means our binary search has landed
		//on the first few of the several consecutive identical cdfValues

		do
		{
			//Then we will perform a forward search to find the next positive pdf. Note that this search cannot fail,
			//because if there are some zero pdfs at the end, their cdfValues will be exactly one, which is higher than
			//the maximum value for our sample, so they will never get selected by the binary search.

			Assert.IsTrue(index < Count - 1);

			lower = upper;
			upper = cdfValues[++index];
		}
		while (lower.Equals(upper));

		return index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void GetBounds(int index, out float lower, out float upper)
	{
		Assert.IsTrue(cdfValues.IsIndexValid(index));
		lower = index == 0 ? 0f : cdfValues[index - 1];
		upper = cdfValues[index];
	}
}