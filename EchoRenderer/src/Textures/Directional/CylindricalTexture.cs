using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Diagnostics;
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

			Assert.IsTrue(size > Int2.zero);
			Float2 sizeR = 1f / size;

			//Fetch temporary buffers
			using var _0 = SpanPool<float>.Fetch(size.Product, out Span<float> weights);
			using var _1 = SpanPool<float>.Fetch(size.x + 1, out Span<float> pastWeights);

			//Get the first luminance weights when discrete points on v = 0
			for (int x = 0; x <= size.x; x++) pastWeights[x] = GetWeight(new Float2(x * sizeR.x, 0f));

			//Fill actual weights by summing the four weights of each pixel corner
			int index = -1;

			for (int y = 1; y <= size.y; y++)
			{
				float sin = MathF.Sin(Scalars.PI * (y - 0.5f) * sizeR.y);
				float previous = GetWeight(new Float2(0f, y * sizeR.y));

				for (int x = 1; x <= size.x; x++)
				{
					ref float bottomLeft = ref pastWeights[x - 1];
					float current = GetWeight(new Float2(x, y) * sizeR);
					float weight = current + previous + bottomLeft + pastWeights[x];

					weights[++index] = weight * sin;

					bottomLeft = previous;
					previous = current;
				}
			}

			//Construct piecewise distribution
			piecewise = new Piecewise2(weights, size.x);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			float GetWeight(in Float2 uv) => Utilities.GetLuminance(texture.Tint.Apply(texture[uv]));
		}

		public Vector128<float> Evaluate(in Float3 direction) => Texture[ToUV(direction)];

		public Vector128<float> Sample(Distro2 distro, out Float3 incident, out float pdf)
		{
			Float2 uv = piecewise.SampleContinuous(distro, out pdf);

			if (!FastMath.Positive(pdf))
			{
				incident = default;
				return Vector128<float>.Zero;
			}

			float angle0 = uv.x * Scalars.TAU;
			float angle1 = uv.y * Scalars.PI;

			FastMath.SinCos(angle0, out float sinT, out float cosT); //Theta
			FastMath.SinCos(angle1, out float sinP, out float cosP); //Phi

			incident = new Float3(-sinP * sinT, -cosP, -sinP * cosT);

			if (sinP <= 0f) pdf = 0f;
			else pdf *= Jacobian / sinP;

			return Texture[uv];
		}

		public float ProbabilityDensity(in Float3 incident)
		{
			Float2 uv = ToUV(incident);
			float cosP = -incident.y;
			float sinP = FastMath.Identity(cosP);

			if (sinP <= 0f) return 0f;

			return piecewise.ProbabilityDensity((Distro2)uv) * Jacobian / sinP;
		}

		static Float2 ToUV(in Float3 direction) => new
		(
			FastMath.FMA(MathF.Atan2(direction.x, direction.z), 0.5f / Scalars.PI, 0.5f),
			FastMath.FMA(MathF.Acos(FastMath.Clamp11(direction.y)), -1f / Scalars.PI, 1f)
		);
	}
}