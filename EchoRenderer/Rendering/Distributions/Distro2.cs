using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Distributions
{
	/// <summary>
	/// A sample on a two dimensional distribution between zero (inclusive) and one (exclusive)
	/// </summary>
	public readonly struct Distro2
	{
		public Distro2(Float2 u) : this(new Distro1(u.x), new Distro1(u.y)) { }

		public Distro2(Distro1 x, Distro1 y)
		{
			this.x = x;
			this.y = y;
		}

		public readonly Distro1 x;
		public readonly Distro1 y;

		/// <summary>
		/// Returns a uniformly sampled point on a unit hemisphere surface.
		/// </summary>
		public Float3 UniformHemisphere => ProjectSphere(x, y);

		/// <summary>
		/// Returns the probability density function for <see cref="UniformHemisphere"/>.
		/// </summary>
		public float UniformHemispherePDF => 1f / Scalars.TAU;

		/// <summary>
		/// Returns a uniformly sampled point on a unit sphere surface.
		/// </summary>
		public Float3 UniformSphere => ProjectSphere(1f - x * 2f, y);

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
				float radius = FastMath.Sqrt0(x);
				float angle = Scalars.TAU * y;
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
				Float2 xy = this;

				if (xy.EqualsExact(Float2.half)) return Float2.zero;
				xy = xy * 2f - Float2.one;

				float radius;
				float angle;

				if (FastMath.Abs(xy.x) > FastMath.Abs(xy.y))
				{
					radius = xy.x;
					angle = Scalars.PI / 4f * xy.y / xy.x;
				}
				else
				{
					radius = xy.y;
					angle = Scalars.PI / 2f * (1f - xy.x / xy.y / 2f);
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

		public static implicit operator Float2(Distro2 distro) => new(distro.x, distro.y);

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