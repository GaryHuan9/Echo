using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Evaluation.Distributions;

/// <summary>
/// A sample on a two dimensional distribution between zero (inclusive) and one (exclusive)
/// </summary>
public readonly struct Sample2D
{
	Sample2D(Float2 u) : this((Sample1D)u.X, (Sample1D)u.Y) { }

	public Sample2D(Sample1D x, Sample1D y)
	{
		this.x = x;
		this.y = y;
	}

	public readonly Sample1D x;
	public readonly Sample1D y;

	/// <summary>
	/// Returns a uniformly sampled point on a unit hemisphere surface.
	/// </summary>
	public Float3 UniformHemisphere => ProjectSphere(x, y);

	/// <summary>
	/// Returns a uniformly sampled point on a unit sphere surface.
	/// </summary>
	public Float3 UniformSphere => ProjectSphere(FastMath.FMA(x, -2f, 1f), y);

	/// <summary>
	/// Returns a uniformly sampled point inside a unit disk.
	/// </summary>
	public Float2 UniformDisk
	{
		get
		{
			float radius = FastMath.Sqrt0(x);
			float angle = Scalars.Tau * y;
			return ProjectDisk(radius, angle);
		}
	}

	/// <summary>
	/// Returns a uniformly sampled barycentric coordinate for a triangle.
	/// NOTE: the pdf is simply one over the area of the triangle.
	/// </summary>
	public Float2 UniformTriangle
	{
		get
		{
			float v = MathF.Sqrt(x);
			return new Float2(1f - v, y * v);
		}
	}

	/// <summary>
	/// Returns a uniformly and concentrically sampled point inside a unit disk.
	/// </summary>
	public Float2 ConcentricDisk
	{
		get
		{
			Float2 xy = this;

			if (xy.EqualsExact(Float2.Half)) return Float2.Zero;
			xy = xy * 2f - Float2.One;

			float radius;
			float angle;

			if (FastMath.Abs(xy.X) > FastMath.Abs(xy.Y))
			{
				radius = xy.X;
				angle = Scalars.Pi / 4f * xy.Y / xy.X;
			}
			else
			{
				radius = xy.Y;
				angle = Scalars.Pi / 2f * FastMath.FMA(xy.X / xy.Y, -0.5f, 1f);
			}

			return ProjectDisk(radius, angle);
		}
	}

	/// <summary>
	/// Returns a cosine weight sampled point on the surface of a unit hemisphere by
	/// first generating a uniform disk as the base and projecting it onto the hemisphere.
	/// </summary>
	public Float3 CosineHemisphere
	{
		get
		{
			Float2 disk = ConcentricDisk;
			float z = disk.SquaredMagnitude;
			return disk.CreateXY(FastMath.Sqrt0(1f - z));
		}
	}

	/// <summary>
	/// The probability density function for <see cref="UniformHemisphere"/>.
	/// </summary>
	public const float UniformHemispherePdf = Scalars.TauR;

	/// <summary>
	/// The probability density function for <see cref="UniformSphere"/>.
	/// </summary>
	public const float UniformSpherePdf = Scalars.TauR / 2f;

	/// <summary>
	/// Maps this <see cref="Sample2D"/> to be between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Range(Int2 max)
	{
		Assert.IsTrue(max >= Int2.Zero);
		return (Int2)((Float2)this * max);
	}

	/// <summary>
	/// Maps this <see cref="Sample2D"/> to be between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Range(Int2 min, Int2 max)
	{
		Assert.IsTrue(min < max);
		return Range(max - min) + min;
	}

	public static implicit operator Float2(Sample2D sample) => new(sample.x, sample.y);
	public static explicit operator Sample2D(Float2 value) => new(value);

	static Float3 ProjectSphere(float z, float u)
	{
		float radius = FastMath.Identity(z);
		float angle = Scalars.Tau * u;
		return ProjectDisk(radius, angle).CreateXY(z);
	}

	static Float2 ProjectDisk(float radius, float angle)
	{
		FastMath.SinCos(angle, out float sin, out float cos);
		return new Float2(cos, sin) * radius;
	}
}