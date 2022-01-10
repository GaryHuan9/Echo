using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Distributions
{
	/// <summary>
	/// A one dimensional piecewise distribution constructed from a function of discrete points.
	/// </summary>
	public class Piecewise1
	{
		public Piecewise1(ReadOnlySpan<float> function)
		{
			int length = function.Length;
			pdfValues = function.ToArray();
			cdfValues = new float[length];
			lengthR = 1f / length;

			//Find the integral across function
			integral = 0f;

			for (int i = 0; i < length; i++)
			{
				Assert.IsFalse(function[i] < 0f); //Probability should not be negative
				cdfValues[i] = integral = FastMath.FMA(function[i], lengthR, integral);
			}

			//Normalize the cumulative distribution function (cdf)
			if (integral.AlmostEquals())
			{
				//If the total integral is zero, it means our function is a constant probability of zero

				for (int i = 0; i < length; i++)
				{
					pdfValues[i] = lengthR;
					cdfValues[i] = i * lengthR;
				}

				integral = 0f;
			}
			else
			{
				//Normalizes the cdf by dividing by the total integral
				float sumR = 1f / integral;

				for (int i = 0; i < length; i++)
				{
					pdfValues[i] *= sumR;
					cdfValues[i] *= sumR;
				}
			}

			cdfValues[length - 1] = 1f;
		}

		/// <summary>
		/// The integral across the input function that was used to construct this <see cref="Piecewise1"/>.
		/// </summary>
		public readonly float integral;

		readonly float[] pdfValues; //Probability density functions
		readonly float[] cdfValues; //Cumulated density functions

		/// <summary>
		/// The reciprocal of <see cref="Length"/>.
		/// </summary>
		readonly float lengthR;

		/// <summary>
		/// The total number of discrete values in this <see cref="Piecewise1"/>.
		/// </summary>
		public int Length => pdfValues.Length;

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at continuous linear intervals based on <paramref name="distro"/>.
		/// </summary>
		public Distro1 SampleContinuous(Distro1 distro, out float pdf)
		{
			FindIndex(distro, out int index);

			float min = index == 0 ? 0f : cdfValues[index - 1];
			float shift = Scalars.InverseLerp(min, cdfValues[index], distro);

			pdf = pdfValues[index];
			return (Distro1)((shift + index) * lengthR);
		}

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at discrete points based on <paramref name="distro"/>.
		/// </summary>
		public int SampleDiscrete(Distro1 distro, out float pdf)
		{
			FindIndex(distro, out int index);
			pdf = pdfValues[index] * lengthR;
			return index;
		}

		/// <summary>
		/// Returns the probability destiny function of this <see cref="Piecewise2"/>
		/// from the <see cref="SampleContinuous"/> <paramref name="distro"/>.
		/// </summary>
		public float ProbabilityDensity(Distro1 distro) => pdfValues[distro.Range(Length)];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void FindIndex(Distro1 distro, out int index)
		{
			index = new ReadOnlySpan<float>(cdfValues).BinarySearch(distro.u);
			if (index < 0) index = ~index;
			Assert.IsTrue(index < Length);
		}
	}
}