using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Pixels
{
	/// <summary>
	/// Mutable struct storing the accumulating render information for one pixel
	/// </summary>
	public struct RenderPixel
	{
		//The color data
		Double3 average;
		Double3 squared;

		//The auxiliary data
		Double3 albedo;
		Double3 normal;

		int accumulation;

		const double MinDeviationThreshold = 0.3d;

		/// <summary>
		/// Returns the color average.
		/// </summary>
		public Float3 Color => (Float3)average;

		/// <summary>
		/// Returns the standard deviation of the pixel.
		/// Based on algorithm described here: https://nestedsoftware.com/2018/03/27/calculating-standard-deviation-on-streaming-data-253l.23919.html
		/// </summary>
		public double Deviation
		{
			get
			{
				double deviation = Math.Sqrt(squared.Average / accumulation);
				double max = Math.Max(average.Average, MinDeviationThreshold);

				return deviation / max;
			}
		}

		/// <summary>
		/// Accumulates the sample <paramref name="value"/> to pixel.
		/// Returns false if the input was rejected because it was invalid.
		/// </summary>
		public bool Accumulate(in PixelWorker.Sample value)
		{
			if (value.IsNaN) return false; //NaN gate

			accumulation++;

			Double3 oldMean = average;
			Double3 newValue = value.colour;

			average += (newValue - oldMean) / accumulation;
			squared += (newValue - average) * (newValue - oldMean);

			albedo += value.albedo;
			normal += value.normal;

			return true;
		}

		public void Store(RenderBuffer buffer, Int2 position)
		{
			buffer[position] = Utilities.ToVector(Utilities.ToColor(Color));

			buffer.SetAlbedo(position, (Float3)(albedo / accumulation));
			buffer.SetNormal(position, (Float3)normal.Normalized);
		}

		readonly struct Double3
		{
			public Double3(double x, double y, double z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			readonly double x;
			readonly double y;
			readonly double z;

			public double Max => Math.Max(x, Math.Max(y, z));
			public double Average => (x + y + z) / 3f;

			public Double3 Normalized
			{
				get
				{
					double length = Math.Sqrt(x * x + y * y + z * z);
					return this / Math.Max(length, double.Epsilon);
				}
			}

			public static Double3 operator +(Double3 first, Double3 second) => new Double3(first.x + second.x, first.y + second.y, first.z + second.z);
			public static Double3 operator -(Double3 first, Double3 second) => new Double3(first.x - second.x, first.y - second.y, first.z - second.z);

			public static Double3 operator *(Double3 first, Double3 second) => new Double3(first.x * second.x, first.y * second.y, first.z * second.z);
			public static Double3 operator /(Double3 first, Double3 second) => new Double3(first.x / second.x, first.y / second.y, first.z / second.z);

			public static Double3 operator *(Double3 first, double second) => new Double3(first.x * second, first.y * second, first.z * second);
			public static Double3 operator /(Double3 first, double second) => new Double3(first.x / second, first.y / second, first.z / second);

			public static Double3 operator *(double first, Double3 second) => new Double3(first * second.x, first * second.y, first * second.z);
			public static Double3 operator /(double first, Double3 second) => new Double3(first / second.x, first / second.y, first / second.z);

			public static implicit operator Double3(Float3 value) => new Double3(value.x, value.y, value.z);
			public static explicit operator Float3(Double3 value) => new Float3((float)value.x, (float)value.y, (float)value.z);
		}
	}
}