using System;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Textures.Directional
{
	public class CylindricalTexture : IDirectionalTexture
	{
		NotNull<Texture> _texture = Texture.black;

		public Texture Texture
		{
			get => _texture;
			set => _texture = value;
		}

		Piecewise2 piecewise;

		/// <summary>
		/// The Jacobian used to convert from uv coordinates to spherical coordinates.
		/// NOTE: we are also missing the one over sin phi term.
		/// </summary>
		const float Jacobian = 1f / Scalars.TAU / Scalars.PI;

		public void Prepare()
		{
			Texture texture = Texture;

			Int2 size = texture.ImportanceSamplingResolution;
			Float2 sizeR = 1f / size.Max(Int2.one);

			int index = -1;

			using var _ = SpanPool<float>.Fetch(size.Product, out Span<float> values);

			for (int y = 0; y < size.y; y++)
			{
				float sin = MathF.Sin(Scalars.PI * (y + 0.5f) * sizeR.y);

				for (int x = 0; x < size.x; x++)
				{
					Float2 uv = new Float2(x, y) * sizeR;

					Vector128<float> color = texture.Tint.Apply(texture[uv]);
					values[++index] = Utilities.GetLuminance(color) * sin;
				}
			}

			piecewise = new Piecewise2(values, size.x);
		}

		public Vector128<float> Evaluate(in Float3 direction) => Texture[ToUV(direction)];

		public Vector128<float> Sample(Distro2 distro, out Float3 incidentWorld, out float pdf)
		{
			Float2 uv = piecewise.SampleContinuous(distro, out pdf);

			if (pdf <= 0f)
			{
				incidentWorld = default;
				return Vector128<float>.Zero;
			}

			float angle0 = uv.x * Scalars.TAU;
			float angle1 = uv.y * Scalars.PI;

			FastMath.SinCos(angle0, out float sinT, out float cosT); //Theta
			FastMath.SinCos(angle1, out float sinP, out float cosP); //Phi

			incidentWorld = new Float3(-sinP * sinT, -cosP, -sinP * cosT);

			if (sinP <= 0f)
			{
				pdf = 0f;
				return Vector128<float>.Zero;
			}

			pdf *= Jacobian / sinP;
			return Texture[uv];
		}

		public float ProbabilityDensity(in Float3 incidentWorld)
		{
			Float2 uv = ToUV(incidentWorld);
			float phi = FastMath.FMA(uv.y, -Scalars.PI, 1f);

			if (phi == 0f) return 0f;
			float sinP = MathF.Sin(phi);

			return piecewise.ProbabilityDensity((Distro2)uv) * Jacobian / sinP;
		}

		static Float2 ToUV(in Float3 direction) => new
		(
			FastMath.FMA(MathF.Atan2(direction.x, direction.z), 0.5f / Scalars.PI, 0.5f),
			FastMath.FMA(MathF.Acos(FastMath.Clamp11(direction.y)), -1f / Scalars.PI, 1f)
		);
	}
}