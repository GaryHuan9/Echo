using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;

namespace EchoRenderer.Rendering.Distributions
{
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
			//Calculate size and create arrays
			size = new Int2(width, function.Length / width);
			Assert.AreEqual(function.Length, size.Product);

			slices = new Piecewise1[size.y];
			using var _ = SpanPool<float>.Fetch(size.y, out Span<float> values);

			//Create single dimensional functions and collect integrals
			for (int y = 0; y < size.y; y++)
			{
				var piecewise = new Piecewise1(function.Slice(y * width, width));

				slices[y] = piecewise;
				values[y] = piecewise.integral;
			}

			integrals = new Piecewise1(values);
			integral = integrals.integral;
		}

		/// <summary>
		/// The total size of discretely defined values in this <see cref="Piecewise2"/>.
		/// </summary>
		public readonly Int2 size;

		/// <summary>
		/// The integral across the input function that was used to construct this <see cref="Piecewise2"/>.
		/// </summary>
		public readonly float integral;

		readonly Piecewise1[] slices;
		readonly Piecewise1 integrals;

		/// <summary>
		/// Samples this <see cref="Piecewise2"/> at continuous linear intervals based on <paramref name="distro"/>.
		/// </summary>
		public Distro2 SampleContinuous(Distro2 distro, out float pdf)
		{
			Distro1 y = integrals.SampleContinuous(distro.y, out float pdfY);
			Distro1 x = slices[y.Range(size.y)].SampleContinuous(distro.x, out pdf);

			pdf *= pdfY;
			return new Distro2(x, y);
		}

		/// <summary>
		/// Samples this <see cref="Piecewise2"/> at discrete points based on <paramref name="distro"/>.
		/// </summary>
		public Int2 SampleDiscrete(Distro2 distro, out float pdf)
		{
			int y = integrals.SampleDiscrete(distro.y, out float pdfY);
			int x = slices[y].SampleDiscrete(distro.x, out pdf);

			pdf *= pdfY;
			return new Int2(x, y);
		}

		/// <summary>
		/// Returns the probability destiny function of this <see cref="Piecewise2"/>
		/// from the <see cref="SampleContinuous"/> <paramref name="distro"/>.
		/// </summary>
		public float ProbabilityDensity(Distro2 distro)
		{
			Piecewise1 slice = slices[distro.y.Range(size.y)];

			float pdfX = slice.ProbabilityDensity(distro.x);
			float pdfY = integrals.ProbabilityDensity(distro.y);

			return pdfX * pdfY;
		}
	}
}