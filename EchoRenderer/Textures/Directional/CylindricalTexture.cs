using System;
using System.Buffers;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

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

		public void Prepare()
		{
			Int2 size = Texture.ImportanceSamplingResolution;

			// ArrayPool<float>.Shared.Rent()
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