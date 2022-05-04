using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;

namespace Echo.Core.Evaluation.Distributions.Discrete;

/// <summary>
/// A two dimensional piecewise distribution constructed from a function of discrete points.
/// </summary>
public readonly struct DiscreteDistribution2D
{
	/// <summary>
	/// Constructs a <see cref="DiscreteDistribution2D"/> from discrete points on a 2D <see cref="function"/>,
	/// which should be provided in an x-axis major order, with <paramref name="width"/> columns.
	/// </summary>
	public DiscreteDistribution2D(ReadOnlySpan<float> function, int width)
	{
		Assert.IsFalse(function.IsEmpty);

		//Calculate size and create arrays
		size = new Int2(width, function.Length / width);
		Assert.AreEqual(function.Length, size.Product);

		slices = new DiscreteDistribution1D[size.Y];
		using var _ = Pool<float>.Fetch(size.Y, out var sums);

		//Create single dimensional functions and collect integrals
		for (int y = 0; y < size.Y; y++)
		{
			var slice = new DiscreteDistribution1D(function.Slice(y * width, width));

			slices[y] = slice;
			sums[y] = slice.sum;
		}

		vertical = new DiscreteDistribution1D(sums);
	}

	/// <summary>
	/// The total size of discretely defined values in this <see cref="DiscreteDistribution2D"/>.
	/// </summary>
	public readonly Int2 size;

	readonly DiscreteDistribution1D[] slices;
	readonly DiscreteDistribution1D vertical;

	/// <summary>
	/// The total sum of the input probability density function.
	/// </summary>
	public float Sum => vertical.sum;

	/// <summary>
	/// The integral across the input probability density function.
	/// </summary>
	public float Integral => vertical.integral;

	/// <summary>
	/// Samples a continuous point based on linear intervals from this <see cref="DiscreteDistribution2D"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample2D"/> used to sample the result.</param>
	/// <returns>The continuous <see cref="Probable{T}"/> point sampled.</returns>
	public Probable<Sample2D> Sample(Sample2D sample)
	{
		Probable<Sample1D> y = vertical.Sample(sample.y);
		Probable<Sample1D> x = slices[y.content.Range(size.Y)].Sample(sample.x);

		return (new Sample2D(x, y), x.pdf * y.pdf);
	}

	/// <summary>
	/// Picks a discrete point from this <see cref="DiscreteDistribution2D"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample2D"/> used to pick the result.</param>
	/// <returns>The discrete <see cref="Probable{T}"/> point picked.</returns>
	public Probable<Int2> Pick(Sample2D sample)
	{
		Probable<int> y = vertical.Pick(sample.y);
		Probable<int> x = slices[y].Pick(sample.x);

		return (new Int2(x, y), x.pdf * y.pdf);
	}

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="result"/> with <see cref="Sample"/>.
	/// </summary>
	/// <param name="result">The selected continuous point.</param>
	/// <returns>The probability density function (pdf) value of the selection.</returns>
	/// <seealso cref="Sample"/>
	public float ProbabilityDensity(Sample2D result)
	{
		ref readonly DiscreteDistribution1D slice = ref slices[result.y.Range(size.Y)];

		float pdfX = slice.ProbabilityDensity(result.x);
		float pdfY = vertical.ProbabilityDensity(result.y);

		return pdfX * pdfY;
	}

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="result"/> with <see cref="Pick"/>.
	/// </summary>
	/// <param name="result">The selected discrete point.</param>
	/// <returns>The probability density function (pdf) value of the selection.</returns>
	/// <seealso cref="Pick"/>
	public float ProbabilityDensity(Int2 result)
	{
		ref readonly DiscreteDistribution1D slice = ref slices[result.Y];

		float pdfX = slice.ProbabilityDensity(result.X);
		float pdfY = vertical.ProbabilityDensity(result.Y);

		return pdfX * pdfY;
	}
}