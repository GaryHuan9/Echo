using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Distributions
{
	/// <summary>
	/// A one dimensional piecewise distribution constructed from a function of discrete probability destiny values.
	/// </summary>
	public class Piecewise1
	{
		public Piecewise1(ReadOnlySpan<float> pdfValues)
		{
			int length = pdfValues.Length;
			cdfValues = new float[length];
			lengthR = 1f / length;

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

				for (int i = 0; i < length; i++) cdfValues[i] = FastMath.FMA(i, lengthR, lengthR);

				sum = 0f; //Sum is still zero though
			}
			else
			{
				//Normalizes the cdf by dividing by the total integral
				float sumR = 1f / sum;

				for (int i = 0; i < length; i++) cdfValues[i] *= sumR;
			}

			cdfValues[length - 1] = 1f;
			integral = sum * lengthR;
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
		/// The reciprocal of <see cref="Length"/>.
		/// </summary>
		readonly float lengthR;

		/// <summary>
		/// The total number of discrete values in this <see cref="Piecewise1"/>.
		/// </summary>
		public int Length => cdfValues.Length;

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at continuous linear intervals based on <paramref name="distro"/>.
		/// </summary>
		public Distro1 SampleContinuous(Distro1 distro, out float pdf)
		{
			//Find index and lower and upper bounds
			FindIndex(distro, out int index);
			GetBounds(index, out float lower, out float upper);

			//Export values
			pdf = (upper - lower) * Length;
			Assert.AreNotEqual(pdf, 0f);

			float shift = Scalars.InverseLerp(lower, upper, distro);
			return (Distro1)((shift + index) * lengthR);
		}

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at discrete points based on <paramref name="distro"/>.
		/// </summary>
		public int SampleDiscrete(Distro1 distro, out float pdf)
		{
			FindIndex(distro, out int index);
			GetBounds(index, out float lower, out float upper);
			pdf = upper - lower;
			return index;
		}

		/// <summary>
		/// Returns the probability destiny function of this <see cref="Piecewise1"/>
		/// if we sampled <paramref name="distro"/> from <see cref="SampleContinuous"/>.
		/// </summary>
		public float ProbabilityDensity(Distro1 distro)
		{
			GetBounds(distro.Range(Length), out float lower, out float upper);
			return (upper - lower) * Length;
		}

		/// <summary>
		/// Returns the probability destiny function of this <see cref="Piecewise1"/>
		/// if we sampled <paramref name="discrete"/> from <see cref="SampleDiscrete"/>.
		/// </summary>
		public float ProbabilityDensity(int discrete)
		{
			GetBounds(discrete, out float lower, out float upper);
			return upper - lower;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void FindIndex(Distro1 distro, out int index)
		{
			index = new ReadOnlySpan<float>(cdfValues).BinarySearch(distro.u);
			if (index < 0) index = ~index;
			Assert.IsTrue(index < Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void GetBounds(int index, out float lower, out float upper)
		{
			Assert.IsTrue(cdfValues.IsIndexValid(index));
			lower = index == 0 ? 0f : cdfValues[index - 1];
			upper = cdfValues[index];
		}
	}
}