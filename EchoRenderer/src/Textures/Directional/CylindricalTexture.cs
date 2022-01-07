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

		public Mode SampleMode { get; set; } = Mode.exact;

		Piecewise2 distribution;

		public void Prepare()
		{
			Texture texture = Texture;

			Int2 size = texture.ImportanceSamplingResolution;
			Float2 sizeR = 1f / (size - Int2.one).Max(Int2.one);

			using var _ = SpanPool<float>.Fetch(size.Product, out Span<float> luminance);

			int index = -1;

			for (int y = 0; y < size.y; y++)
			{
				float v = sizeR.y * y;

				for (int x = 0; x < size.x; x++)
				{
					float u = sizeR.x * x;

					var color = texture[new Float2(u, v) * sizeR];
					luminance[++index] = Utilities.GetLuminance(color);
				}
			}

			distribution = new Piecewise2(luminance, size.x);
		}

		public Vector128<float> Evaluate(in Float3 direction)
		{
			Float2 uv = SampleMode switch
			{
				Mode.exact => new Float2
				(
					0.5f + MathF.Atan2(direction.z, direction.x) * (0.5f / Scalars.PI),
					1f - MathF.Acos(FastMath.Clamp11(direction.y)) * (1f / Scalars.PI)
				),
				Mode.height => new Float2(0f, direction.y * 0.5f + 0.5f),
				_ => throw ExceptionHelper.Invalid(nameof(SampleMode), SampleMode, InvalidType.unexpected)
			};

			Prepare();

			return Texture[uv];
		}

		public enum Mode : byte
		{
			/// <summary>
			/// Evaluates <see cref="Texture"/> based on exact solid angle of directions.
			/// </summary>
			exact,

			/// <summary>
			/// Evaluates <see cref="Texture"/> only based on the Y component of directions.
			/// </summary>
			height
		}
	}
}