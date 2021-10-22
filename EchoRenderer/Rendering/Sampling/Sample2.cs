using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public readonly struct Sample2
	{
		public Sample2(float x, float y) : this(new Float2(x, y)) { }

		public Sample2(Float2 u)
		{
			Assert.IsTrue(Float2.zero <= u);
			Assert.IsTrue(Float2.one > u);
			this.u = u;
		}

		readonly Float2 u;

		public float X => u.x;
		public float Y => u.x;

		public Float3 UniformHemisphere    => ProjectSphere(u.x, u.y);
		public float  UniformHemispherePdf => 1f / Scalars.TAU;

		public Float3 UniformSphere    => ProjectSphere(1f - u.x * 2f, u.y);
		public float  UniformSpherePdf => 1f / 2f / Scalars.TAU;

		public Float2 UniformDisk
		{
			get
			{
				float radius = MathF.Sqrt(u.x);
				float angle  = Scalars.TAU * u.y;
				return ProjectDisk(radius, angle);
			}
		}

		public Float2 ConcentricDisk
		{
			get
			{
				if (u.EqualsExact(Float2.half)) return Float2.zero;
				Float2 mapped = u * 2f - Float2.one;

				float radius;
				float angle;

				if (Math.Abs(mapped.x) > Math.Abs(mapped.y))
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

		public Float3 CosineHemisphere
		{
			get
			{
				Float2 disk = ConcentricDisk;
				float  z    = disk.SquaredMagnitude;
				return disk.CreateXY(MathF.Sqrt(1f - z));
			}
		}

		static Float3 ProjectSphere(float z, float u)
		{
			float radius = MathF.Sqrt(1f - z * z);
			float angle  = Scalars.TAU * u;
			return new Float3(radius * MathF.Cos(angle), radius * MathF.Sin(angle), z);
		}

		static Float2 ProjectDisk(float radius, float angle) => new Float2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
	}
}