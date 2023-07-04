using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Evaluation.Sampling;

/// <summary>
/// A sample on a two dimensional distribution between zero (inclusive) and one (exclusive)
/// </summary>
public readonly struct Sample2D
{
	public Sample2D(Sample1D x, Sample1D y)
	{
		this.x = x;
		this.y = y;
	}

	public Sample2D(float x, float y) : this((Sample1D)x, (Sample1D)y) { }

	Sample2D(Float2 u) : this((Sample1D)u.X, (Sample1D)u.Y) { }

	public readonly Sample1D x;
	public readonly Sample1D y;

	/// <summary>
	/// Returns a uniformly sampled point on a unit hemisphere surface.
	/// </summary>
	/// <remarks>The hemisphere points in the <see cref="Float3.Forward"/>
	/// direction (i.e. the returned Z axis is always positive).</remarks>
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
	/// </summary>
	/// <remarks>The probability density function is simply one over the area of the triangle.</remarks>
	public Float2 UniformTriangle
	{
		get
		{
			float v = FastMath.Sqrt0(x);
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
			float xValue = FastMath.FMA(x, 2f, -1f);
			float yValue = FastMath.FMA(y, 2f, -1f);

			if (FastMath.AlmostZero(xValue) && FastMath.AlmostZero(yValue)) return Float2.Zero;

			float radius;
			float angle;

			if (FastMath.Abs(xValue) > FastMath.Abs(yValue))
			{
				radius = xValue;
				angle = Scalars.Pi / 4f * yValue / xValue;
			}
			else
			{
				radius = yValue;
				angle = FastMath.FMA(xValue / yValue, Scalars.Pi / -4f, Scalars.Pi / 2f);
			}

			return ProjectDisk(radius, angle);
		}
	}

	/// <summary>
	/// Returns a cosine weight sampled point on the surface of a unit hemisphere by
	/// first generating a uniform disk as the base and projecting it onto the hemisphere.
	/// </summary>
	/// <remarks>The orientation of the hemisphere is identical to that of <see cref="UniformHemisphere"/>.</remarks>
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
	public const float UniformSpherePdf = UniformHemispherePdf / 2f;

	/// <summary>
	/// Returns a uniformly sampled direction on a unit cone with a specific vertical opening angle.
	/// </summary>
	/// <param name="cosMaxP">The cosine of the maximum phi vertical opening angle.</param>
	/// <remarks>The orientation of the cone is identical to that of <see cref="UniformHemisphere"/>.</remarks>
	public Float3 UniformCone(float cosMaxP)
	{
		float cosP = FastMath.FMA(cosMaxP - 1f, x, 1f);
		return ProjectSphere(cosP, y);
	}

	/// <summary>
	/// Maps this <see cref="Sample2D"/> to be between zero (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Range(Int2 max)
	{
		Ensure.IsTrue(max >= Int2.Zero);
		return (Int2)((Float2)this * max);
	}

	/// <summary>
	/// Maps this <see cref="Sample2D"/> to be between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// </summary>
	public Int2 Range(Int2 min, Int2 max)
	{
		Ensure.IsTrue(min < max);
		return Range(max - min) + min;
	}

	/// <summary>
	/// The probability density function for <see cref="UniformCone"/>.
	/// </summary>
	/// <param name="cosMaxP">The cosine of the maximum phi vertical angle.</param>
	public static float UniformConePdf(float cosMaxP) => Scalars.TauR / (1f - cosMaxP);

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