using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Distributions
{
	/// <summary>
	/// A sample on a two dimensional distribution between zero (inclusive) and one (exclusive)
	/// </summary>
	public readonly struct Distro2
	{
		public Distro2(float x, float y) : this(new Float2(x, y)) { }

		public Distro2(Float2 u) => this.u = new Float2
									(
										FastMath.ClampEpsilon(u.x),
										FastMath.ClampEpsilon(u.y)
									);

		public readonly Float2 u;

		/// <summary>
		/// Returns a uniformly sampled point on a unit hemisphere surface.
		/// </summary>
		public Float3 UniformHemisphere => ProjectSphere(u.x, u.y);

		/// <summary>
		/// Returns the probability density function for <see cref="UniformHemisphere"/>.
		/// </summary>
		public float UniformHemispherePDF => 1f / Scalars.TAU;

		/// <summary>
		/// Returns a uniformly sampled point on a unit sphere surface.
		/// </summary>
		public Float3 UniformSphere => ProjectSphere(1f - u.x * 2f, u.y);

		/// <summary>
		/// Returns the probability density function for <see cref="UniformSphere"/>.
		/// </summary>
		public float UniformSpherePDF => 1f / 2f / Scalars.TAU;

		/// <summary>
		/// Returns a uniformly sampled point inside a unit disk.
		/// </summary>
		public Float2 UniformDisk
		{
			get
			{
				float radius = FastMath.Sqrt0(u.x);
				float angle = Scalars.TAU * u.y;
				return ProjectDisk(radius, angle);
			}
		}

		/// <summary>
		/// Returns a uniformly and concentrically sampled point inside a unit disk.
		/// </summary>
		public Float2 ConcentricDisk
		{
			get
			{
				if (u.EqualsExact(Float2.half)) return Float2.zero;
				Float2 mapped = u * 2f - Float2.one;

				float radius;
				float angle;

				if (FastMath.Abs(mapped.x) > FastMath.Abs(mapped.y))
				{
					radius = mapped.x;
					angle = Scalars.PI / 4f * mapped.y / mapped.x;
				}
				else
				{
					radius = mapped.y;
					angle = Scalars.PI / 2f * (1f - mapped.x / mapped.y / 2f);
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

		static Float3 ProjectSphere(float z, float u)
		{
			float radius = FastMath.Identity(z);
			float angle = Scalars.TAU * u;
			return ProjectDisk(radius, angle).CreateXY(z);
		}

		static Float2 ProjectDisk(float radius, float angle)
		{
			float cos = MathF.Cos(angle);
			float sin = FastMath.Identity(cos);

			//We use the trigonometry identity to calculate sine because square root is faster than sine

			return new Float2(cos, sin) * radius;
		}
	}
}