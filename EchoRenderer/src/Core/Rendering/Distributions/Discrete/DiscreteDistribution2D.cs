using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;

namespace EchoRenderer.Core.Rendering.Distributions.Discrete;

/// <summary>
/// A two dimensional piecewise distribution constructed from a function of discrete points.
/// </summary>
public class DiscreteDistribution2D
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
	/// Samples this <see cref="DiscreteDistribution2D"/> at continuous linear intervals
	/// based on <paramref name="sample"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public Probable<Sample2D> Sample(Sample2D sample)
	{
		Probable<Sample1D> y = vertical.Sample(sample.y);
		Probable<Sample1D> x = slices[y.content.Range(size.Y)].Sample(sample.x);

		return (new Sample2D(x, y), x.pdf * y.pdf);
	}

	/// <summary>
	/// Finds a discrete point from this <see cref="DiscreteDistribution2D"/> based on
	/// <paramref name="sample"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public Probable<Int2> Find(Sample2D sample)
	{
		Probable<int> y = vertical.Find(sample.y);
		Probable<int> x = slices[y].Find(sample.x);

		return (new Int2(x, y), x.pdf * y.pdf);
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="DiscreteDistribution2D"/>
	/// if we sampled <paramref name="sample"/> from <see cref="Sample"/>.
	/// </summary>
	public float ProbabilityDensity(Sample2D sample)
	{
		DiscreteDistribution1D slice = slices[sample.y.Range(size.Y)];

		float pdfX = slice.ProbabilityDensity(sample.x);
		float pdfY = vertical.ProbabilityDensity(sample.y);

		return pdfX * pdfY;
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="DiscreteDistribution2D"/>
	/// if we sampled <paramref name="point"/> from <see cref="Find"/>.
	/// </summary>
	public float ProbabilityDensity(Int2 point)
	{
		DiscreteDistribution1D slice = slices[point.Y];

		float pdfX = slice.ProbabilityDensity(point.X);
		float pdfY = vertical.ProbabilityDensity(point.Y);

		return pdfX * pdfY;
	}
}