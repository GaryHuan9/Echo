using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	public class SamplePattern
	{
		public SamplePattern(int length)
		{
			float goldenRatio = (1f + (float)Math.Sqrt(5d)) / 2f;
			offsets = new Float2[length];

			for (float i = 0.5f; i < length; i++)
			{
				float angle = Scalars.TAU * goldenRatio * i;
				Float2 sample = new Float2((float)Math.Cos(angle), (float)Math.Sin(angle));

				offsets[(int)i] = sample * (float)Math.Sqrt(i / length) / 2f + Float2.half;
			}
		}

		readonly Float2[] offsets;
		public int Length => offsets.Length;

		/// <summary>
		/// Returns a 2D offset between 0 to 1 that indicates the percent in a pixel to sample.
		/// </summary>
		public Float2 this[int sample] => offsets[sample];
	}
}