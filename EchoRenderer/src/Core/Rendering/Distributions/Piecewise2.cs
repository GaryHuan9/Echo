using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Memory;

namespace EchoRenderer.Core.Rendering.Distributions;

/// <summary>
/// A two dimensional piecewise distribution constructed from a function of discrete points.
/// </summary>
public class Piecewise2
{
	/// <summary>
	/// Constructs a <see cref="Piecewise2"/> from discrete points on a 2D <see cref="function"/>,
	/// which should be provided in an x-axis major order, with <paramref name="width"/> columns.
	/// </summary>
	public Piecewise2(ReadOnlySpan<float> function, int width)
	{
		Assert.IsFalse(function.IsEmpty);

		//Calculate size and create arrays
		size = new Int2(width, function.Length / width);
		Assert.AreEqual(function.Length, size.Product);

		slices = new Piecewise1[size.y];
		using var _ = Pool<float>.Fetch(size.y, out Span<float> sums);

		//Create single dimensional functions and collect integrals
		for (int y = 0; y < size.y; y++)
		{
			var piecewise = new Piecewise1(function.Slice(y * width, width));

			slices[y] = piecewise;
			sums[y] = piecewise.sum;
		}

		vertical = new Piecewise1(sums);
	}

	/// <summary>
	/// The total size of discretely defined values in this <see cref="Piecewise2"/>.
	/// </summary>
	public readonly Int2 size;

	readonly Piecewise1[] slices;
	readonly Piecewise1 vertical;

	/// <summary>
	/// The total sum of the input probability density function.
	/// </summary>
	public float Sum => vertical.sum;

	/// <summary>
	/// The integral across the input probability density function.
	/// </summary>
	public float Integral => vertical.integral;

	/// <summary>
	/// Samples this <see cref="Piecewise2"/> at continuous linear intervals
	/// based on <paramref name="distro"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public Distro2 Sample(Distro2 distro, out float pdf)
	{
		Distro1 y = vertical.Sample(distro.y, out float pdfY);
		Distro1 x = slices[y.Range(size.y)].Sample(distro.x, out pdf);

		pdf *= pdfY;
		return new Distro2(x, y);
	}

	/// <summary>
	/// Finds a discrete point from this <see cref="Piecewise2"/> based on
	/// <paramref name="distro"/> and outputs the <paramref name="pdf"/>.
	/// </summary>
	public Int2 Find(Distro2 distro, out float pdf)
	{
		int y = vertical.Find(distro.y, out float pdfY);
		int x = slices[y].Find(distro.x, out pdf);

		pdf *= pdfY;
		return new Int2(x, y);
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="Piecewise2"/>
	/// if we sampled <paramref name="distro"/> from <see cref="Sample"/>.
	/// </summary>
	public float ProbabilityDensity(Distro2 distro)
	{
		Piecewise1 slice = slices[distro.y.Range(size.y)];

		float pdfX = slice.ProbabilityDensity(distro.x);
		float pdfY = vertical.ProbabilityDensity(distro.y);

		return pdfX * pdfY;
	}

	/// <summary>
	/// Returns the probability destiny function of this <see cref="Piecewise2"/>
	/// if we sampled <paramref name="point"/> from <see cref="Find"/>.
	/// </summary>
	public float ProbabilityDensity(Int2 point)
	{
		Piecewise1 slice = slices[point.y];

		float pdfX = slice.ProbabilityDensity(point.x);
		float pdfY = vertical.ProbabilityDensity(point.y);

		return pdfX * pdfY;
	}
}