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
			values = function.ToArray();
			cdf = new float[length];
			countR = 1f / length;

			//Find the integral across function
			float sum = 0f;

			for (int i = 0; i < length; i++)
			{
				Assert.IsFalse(function[i] < 0f); //Probability should not be negative
				cdf[i] = sum = FastMath.FMA(function[i], countR, sum);
			}

			integral = cdf[length - 1];
			integralR = 1f / integral;

			//Normalize the cumulative distribution function (cdf)
			if (integral.AlmostEquals())
			{
				//If the total integral is zero, it means our function is a constant probability of zero

				for (int i = 0; i < length; i++)
				{
					values[i] = 0f;
					cdf[i] = i * countR;
				}
			}
			else
			{
				//Normalizes the cdf by dividing by the total integral
				for (int i = 0; i < length; i++) cdf[i] *= integralR;
			}

			cdf[length - 1] = 1f;
		}

		/// <summary>
		/// The integral across the function defined by <see cref="values"/>.
		/// </summary>
		public readonly float integral;

		readonly float[] values;
		readonly float[] cdf;

		/// <summary>
		/// The reciprocal of <see cref="integral"/>.
		/// </summary>
		readonly float integralR;

		/// <summary>
		/// The reciprocal of <see cref="Length"/>.
		/// </summary>
		readonly float countR;

		/// <summary>
		/// The total number of values in thi s<see cref="Piecewise1"/>.
		/// </summary>
		public int Length => values.Length;

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at continuous linear intervals based on <paramref name="distro"/>.
		/// </summary>
		public Distro1 SampleContinuous(Distro1 distro, out float pdf)
		{
			FindIndex(distro, out int index);

			float min = index == 0 ? 0f : cdf[index - 1];
			float shift = Scalars.InverseLerp(min, cdf[index], distro);

			pdf = values[index] * integralR;
			return (Distro1)((shift + index) * countR);
		}

		/// <summary>
		/// Samples this <see cref="Piecewise1"/> at discrete points based on <paramref name="distro"/>.
		/// </summary>
		public int SampleDiscrete(Distro1 distro, out float pdf)
		{
			FindIndex(distro, out int index);
			pdf = values[index] * integralR * countR;
			return index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void FindIndex(Distro1 distro, out int index)
		{
			index = new ReadOnlySpan<float>(cdf).BinarySearch(distro.u);
			if (index < 0) index = ~index;
			Assert.IsTrue(index < Length);
		}
	}
}