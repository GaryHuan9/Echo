using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.Rendering.Distributions.Discrete;

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
			Assert.IsFalse(pdfValues[i] < 0f); //pdf should not be negative
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
		float gap = upper - lower;
		Assert.AreNotEqual(gap, 0f);
		pdf = gap * Count;

		float shift = (sample - lower) / gap;
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
		int index = BinarySearch(cdfValues, sample);

		//Majority of the times we will simply exit because we are between two anchors
		Assert.IsTrue(~index < Count);
		if (index < 0) return ~index;

		//When we landed exactly on an anchor, we need to perform some special checks
		//So we move onto the slower path and invokes a special method to fix the index

		return FixIndex(index); //NOTE: we explicitly separate this out into a method for performance
	}

	int FixIndex(int index)
	{
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
		while (lower == upper);

		return index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void GetBounds(int index, out float lower, out float upper)
	{
		Assert.IsTrue(cdfValues.IsIndexValid(index));
		lower = index == 0 ? 0f : cdfValues[index - 1];
		upper = cdfValues[index];
	}

	/// <summary>
	/// Efficient binary search referenced from C# <see cref="MemoryExtensions.BinarySearch{T, TComparable}(Span{T},TComparable)"/>
	/// with significant changes specifically modified to only support <see cref="float"/> values to avoid comparer overhead.
	/// </summary>
	static int BinarySearch(float[] array, float value)
	{
		uint head = 0u;
		uint tail = (uint)array.Length;
		ref float origin = ref array[0];

		while (head < tail)
		{
			uint index = (tail + head) >> 1;

			var current = Unsafe.Add(ref origin, index);
			if (current == value) return (int)index;

			if (current > value) tail = index;
			else head = index + 1u;
		}

		//NOTE: we can optimize this even further if needed:
		//https://github.com/scandum/binary_search
		//https://news.ycombinator.com/item?id=23893366

		return (int)~head;
	}
}