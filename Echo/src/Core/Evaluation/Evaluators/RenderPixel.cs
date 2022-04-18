using System;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Evaluators;

/// <summary>
/// Mutable struct storing the accumulating render information for one pixel.
/// </summary>
public struct RenderPixel
{
	Summation average;
	Summation squared;

	int count;

	/// <summary>
	/// Returns the color average.
	/// </summary>
	public RGB128 Value => (RGB128)average.Result;

	/// <summary>
	/// Returns the current sample variance for this <see cref="RenderPixel"/>. Based on algorithm described here:
	/// https://nestedsoftware.com/2018/03/27/calculating-standard-deviation-on-streaming-data-253l.23919.html
	/// </summary>
	public float Variance
	{
		get
		{
			int divisor = Math.Max(1, count - 1);
			Float4 total = squared.Result / divisor;
			return ((RGB128)total).Luminance;
		}
	}

	/// <summary>
	/// Accumulates the sample <paramref name="value"/> to pixel.
	/// Returns false if the input was rejected because it was invalid.
	/// </summary>
	public bool Accumulate(in RGB128 value)
	{
		Float4 data = value;
		if (!float.IsFinite(data.Sum)) return false; //Gates degenerate values

		++count;

		Summation delta = average - data;
		average -= delta / (Float4)count;
		squared += delta * (data - average.Result);

		return true;
	}

	public void Store(RenderBuffer buffer, Int2 position)
	{
		buffer[position] = Value;

		// double inverse = 1d / accumulation;
		// buffer.SetAlbedo(position, (Float3)(albedo * inverse));
		// buffer.SetNormal(position, (Float3)normal.Normalized);
		// buffer.SetZDepth(position, (float)(zDepth * inverse));
	}
}