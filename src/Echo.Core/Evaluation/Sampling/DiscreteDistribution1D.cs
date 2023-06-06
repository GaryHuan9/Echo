using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Core.Evaluation.Sampling;

/// <summary>
/// A one dimensional piecewise distribution constructed from a function of discrete probability density values.
/// </summary>
public readonly struct DiscreteDistribution1D
{
	public DiscreteDistribution1D(ReadOnlySpan<float> pdfValues)
	{
		// Ensure.IsFalse(pdfValues.IsEmpty);
		//
		// int length = pdfValues.Length;
		// cdfValues = new float[length];
		// countR = 1f / length;
		//
		// //Find the total sum and initialize cdf
		// double rolling = 0d;
		//
		// for (int i = 0; i < length; i++)
		// {
		// 	Ensure.IsFalse(pdfValues[i] < 0f); //pdf should not be negative
		// 	cdfValues[i] = (float)(rolling += pdfValues[i]);
		// }
		//
		// sum = (float)rolling;
		//
		// //Normalize the cdf
		// if (FastMath.AlmostZero(sum))
		// {
		// 	//If the total sum is zero, it means our function has a constant probability of zero, which is
		// 	//technically not a correct function, so we will handle it like a non-zero constant function.
		//
		// 	for (int i = 0; i < length; i++) cdfValues[i] = FastMath.FMA(i, countR, countR);
		//
		// 	sum = 0f; //Sum is still zero though
		// }
		// else
		// {
		// 	//Normalizes the cdf by dividing by the total integral
		// 	float sumR = 1f / sum;
		//
		// 	for (int i = 0; i < length; i++) cdfValues[i] *= sumR;
		// }
		//
		// integral = sum * countR;
		//
		// //Assign the last identical values to one to ensure no leaking when sampling
		// int index = length - 1;
		// float last = cdfValues[index];
		//
		// do cdfValues[index] = 1f;
		// while (--index > 0 && last == cdfValues[index]);

		Count = pdfValues.Length;
		List<float> weightsList = new List<float>();
		weightsList.AddRange(pdfValues.ToArray());

		sum = weightsList.Sum();
		float averageSampleWeight = sum / this.Count;

		List<Bin> generatedBins = new List<Bin>();
		for (int i = 0; i < this.Count; i++)
		{
			weightsList.Sort((x,y) => y.CompareTo(x));
			int lowestWeightedSampleIndex = weightsList.Count - 1;
			
			Bin bin = new Bin();
			bin.i = pdfValues.BinarySearch(weightsList[lowestWeightedSampleIndex]);
			bin.j = pdfValues.BinarySearch(weightsList[0]);
			bin.threshold = weightsList[lowestWeightedSampleIndex] / averageSampleWeight;
			generatedBins.Add(bin);
			
			weightsList[0] -= averageSampleWeight - weightsList[lowestWeightedSampleIndex];
			weightsList.RemoveAt(lowestWeightedSampleIndex);
		}

		countR = 1.0f / Count;
		integral = sum * countR;
		
		aliasTable = generatedBins.ToArray();
		weights = pdfValues.ToArray();
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
	//readonly float[] cdfValues;

	/// <summary>
	/// The reciprocal of <see cref="Count"/>.
	/// </summary>
	readonly float countR;

	readonly Bin[] aliasTable;
	readonly float[] weights;

	/// <summary>
	/// The total number of discrete values in this <see cref="DiscreteDistribution1D"/>.
	/// </summary>
	public readonly int Count;
	

	/// <summary>
	/// Samples a continuous value based on linear intervals from this <see cref="DiscreteDistribution1D"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample1D"/> used to sample the result.</param>
	/// <returns>The continuous <see cref="Probable{T}"/> value sampled.</returns>
	public Probable<Sample1D> Sample(Sample1D sample)
	{
		// //Find index and lower and upper bounds
		// int index = FindIndex(sample);
		// GetBounds(index, out float lower, out float upper);
		//
		// //Export values
		// float gap = upper - lower;
		// Ensure.AreNotEqual(gap, 0f);
		//
		// float shift = (sample - lower) / gap + index;
		// Sample1D result = (Sample1D)(shift * countR);
		// return new Probable<Sample1D>(result, gap * Count);

		Random random = new Random();
		int index = random.Next(Count);
		float value = SystemPrng.Shared.Next1();
		int resultIndex = value >= aliasTable[index].threshold ? aliasTable[index].i : aliasTable[index].j;
		return new Probable<Sample1D>((Sample1D)resultIndex, weights[resultIndex]);

	}

	/// <summary>
	/// Picks a discrete value from this <see cref="DiscreteDistribution1D"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample1D"/> used to pick the result.</param>
	/// <returns>The discrete <see cref="Probable{T}"/> value picked.</returns>
	public Probable<int> Pick(Sample1D sample)
	{
		// int index = FindIndex(sample);
		// GetBounds(index, out float lower, out float upper);
		// return new Probable<int>(index, upper - lower);
		int index = (int)SystemPrng.Shared.NextUInt32();
		float value = SystemPrng.Shared.Next1();
		int resultIndex = value >= aliasTable[index].threshold ? aliasTable[index].i : aliasTable[index].j;
		return new Probable<int>(resultIndex, weights[resultIndex]);
	}

	/// <inheritdoc cref="Pick(Sample1D)"/>
	/// <remarks>Note that the input <see cref="Sample1D"/> is a reference; after the usage by this method, it will be
	/// assigned to a new uniform and unbiased <see cref="Sample1D"/> value through the <see cref="Sample1D.Stretch"/>
	/// operation, to be used again.</remarks>
	public Probable<int> Pick(ref Sample1D sample)
	{
		// int index = FindIndex(sample);
		// GetBounds(index, out float lower, out float upper);
		// sample = sample.Stretch(lower, upper);
		// return new Probable<int>(index, upper - lower);
		int index = (int)SystemPrng.Shared.NextUInt32();
		float value = SystemPrng.Shared.Next1();
		int resultIndex = value >= aliasTable[index].threshold ? aliasTable[index].i : aliasTable[index].j;
		return new Probable<int>(resultIndex, weights[resultIndex]);
	}

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="result"/> with <see cref="Sample"/>.
	/// </summary>
	/// <param name="result">The selected continuous value.</param>
	/// <returns>The probability density function (pdf) value of the selection.</returns>
	/// <seealso cref="Sample"/>
	public float ProbabilityDensity(Sample1D result)
	{
		//GetBounds(result.Range(Count), out float lower, out float upper);
		//return (upper - lower) * Count;
		return 0;
	}

	/// <summary>
	/// Calculates the pmf of selecting <paramref name="result"/> with <see cref="Pick(Sample1D)"/>.
	/// </summary>
	/// <param name="result">The selected discrete value.</param>
	/// <returns>The probability mass function (pmf) value of the selection.</returns>
	/// <seealso cref="Pick(Sample1D)"/>
	public float ProbabilityMass(int result)
	{
		//GetBounds(result, out float lower, out float upper);
		//return upper - lower;
		return 0;
	}

	/*[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int FindIndex(Sample1D sample)
	{
		int index = BinarySearch(cdfValues, sample);
		
		//Majority of the times we will simply exit because we are between two anchors
		Ensure.IsTrue(~index < Count);
		if (index < 0) return ~index;
		
		//When we landed exactly on an anchor, we need to perform some special checks
		//So we move onto the slower path and invokes a special method to fix the index
		
		return FixIndex(index); //NOTE: we explicitly separate this out into a method for performance
	}*/

	/*int FixIndex(int index)
	{
		GetBounds(index, out float lower, out float upper);
		
		float pdf = upper - lower;
		if (pdf > 0f) return index + 1;
		
		//If our pdf is zero, then it means our binary search has landed
		//on the first few of the several consecutive identical cdfValues
		
		do
		{
			//Then we will perform a forward search to find the next positive pdf. Note that this search cannot fail,
			//because if there are some zero pdfs at the end, their cdfValues will be exactly one, which is higher than
			//the maximum value for our sample, so they will never get selected by the binary search.
		
			Ensure.IsTrue(index < Count - 1);
		
			lower = upper;
			upper = cdfValues[++index];
		}
		while (lower == upper);
		
		return index;
	}*/

	/*[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void GetBounds(int index, out float lower, out float upper)
	{
		Ensure.IsTrue(index >= 0);
		Ensure.IsTrue(index < cdfValues.Length);
		lower = index == 0 ? 0f : cdfValues[index - 1];
		upper = cdfValues[index];
	}*/

	/// <summary>
	/// Efficient binary search referenced from C# <see cref="MemoryExtensions.BinarySearch{T, TComparable}(Span{T},TComparable)"/>
	/// with significant changes specifically modified to only support <see cref="float"/> values to avoid comparer overhead.
	/// </summary>
	/*static int BinarySearch(float[] array, float value)
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
	}*/

	struct Bin
	{
		public float threshold;
		public int i;
		public int j;
	}
}