using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.Rendering.Pixels;

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
	double zDepth;

	int accumulation;

	const double MinDeviationThreshold = 0.08d;

	/// <summary>
	/// Returns the color average.
	/// </summary>
	public Float3 Color => (Float3)average;

	/// <summary>
	/// Returns the standard deviation divided by the average of the pixel. Based on algorithm described here:
	/// https://nestedsoftware.com/2018/03/27/calculating-standard-deviation-on-streaming-data-253l.23919.html
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

		++accumulation;

		Double3 oldMean = average;
		Double3 newValue = value.colour;

		average += (newValue - oldMean) / accumulation;
		squared += (newValue - average) * (newValue - oldMean);

		albedo += value.albedo;
		normal += value.normal;
		zDepth += value.zDepth;

		return true;
	}

	public void Store(RenderBuffer buffer, Int2 position)
	{
		buffer[position] = Utilities.ToVector(Utilities.ToColor(Color));

		double inverse = 1d / accumulation;

		buffer.SetAlbedo(position, (Float3)(albedo * inverse));
		buffer.SetNormal(position, (Float3)normal.Normalized);
		buffer.SetZDepth(position, (float)(zDepth * inverse));
	}

	[StructLayout(LayoutKind.Explicit, Size = 32)]
	readonly struct Double3
	{
		Double3(double x, double y, double z)
		{
			Unsafe.SkipInit(out vector);

			this.x = x;
			this.y = y;
			this.z = z;
			zero = 0d;
		}

		Double3(Vector256<double> vector)
		{
			Unsafe.SkipInit(out x);
			Unsafe.SkipInit(out y);
			Unsafe.SkipInit(out z);

			this.vector = vector;
			zero = 0d;
		}

		[FieldOffset(00)] readonly double x;
		[FieldOffset(08)] readonly double y;
		[FieldOffset(16)] readonly double z;
		[FieldOffset(24)] readonly double zero; //Unused component, must always be zero

		[FieldOffset(0)] readonly Vector256<double> vector;

		public double Average => (x + y + z) / 3d;

		public unsafe Double3 Normalized
		{
			get
			{
				double magnitude;

				if (Avx.IsSupported)
				{
					Vector256<double> squared = Avx.Multiply(vector, vector);

					double* p = (double*)&squared;
					magnitude = p[0] + p[1] + p[2];
				}
				else magnitude = x * x + y * y + z * z;

				return this / Math.Max(Math.Sqrt(magnitude), double.Epsilon);
			}
		}

		public static Double3 operator +(in Double3 value, in Double3 other) => Add(value, other);
		public static Double3 operator -(in Double3 value, in Double3 other) => Subtract(value, other);

		public static Double3 operator *(in Double3 value, in Double3 other) => Multiply(value.vector, other.vector);
		public static Double3 operator /(in Double3 value, in Double3 other) => Divide(value.vector, other.vector);

		public static Double3 operator *(in Double3 value, double other) => Multiply(value.vector, Vector256.Create(other));
		public static Double3 operator /(in Double3 value, double other) => Divide(value.vector, Vector256.Create(other));

		public static Double3 operator *(double value, in Double3 other) => Multiply(Vector256.Create(value), other.vector);
		public static Double3 operator /(double value, in Double3 other) => Divide(Vector256.Create(value), other.vector);

		public static implicit operator Double3(in Float3 value) => new(value.x, value.y, value.z);
		public static explicit operator Float3(in Double3 value) => new((float)value.x, (float)value.y, (float)value.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Double3 Add(in Double3 value, in Double3 other)
		{
			if (Avx.IsSupported) return new Double3(Avx.Add(value.vector, other.vector));
			return new Double3(value.x + other.x, value.y + other.y, value.z + other.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Double3 Subtract(in Double3 value, in Double3 other)
		{
			if (Avx.IsSupported) return new Double3(Avx.Subtract(value.vector, other.vector));
			return new Double3(value.x - other.x, value.y - other.y, value.z - other.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static unsafe Double3 Multiply(in Vector256<double> value, in Vector256<double> other)
		{
			if (Avx.IsSupported) return new Double3(Avx.Multiply(value, other));

			Vector256<double> copy0 = value;
			Vector256<double> copy1 = other;

			double* p0 = (double*)&copy0;
			double* p1 = (double*)&copy1;

			return new Double3(p0[0] * p1[0], p0[1] * p1[1], p0[2] * p1[2]);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static unsafe Double3 Divide(in Vector256<double> value, in Vector256<double> other)
		{
			if (Avx.IsSupported) return new Double3(Avx.Divide(value, other));

			Vector256<double> copy0 = value;
			Vector256<double> copy1 = other;

			double* p0 = (double*)&copy0;
			double* p1 = (double*)&copy1;

			return new Double3(p0[0] / p1[0], p0[1] / p1[1], p0[2] / p1[2]);
		}
	}
}