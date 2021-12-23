using System;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures.Directional
{
	public class CylindricalTexture : DirectionalTexture
	{
		public CylindricalTexture() : this(Texture.black) { }
		public CylindricalTexture(Texture texture) => Texture = texture;

		NotNull<Texture> _texture;

		public Texture Texture
		{
			get => _texture;
			set => _texture = value;
		}

		public Mode SampleMode { get; set; } = Mode.exact;

		public override Vector128<float> Sample(in Float3 direction)
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

			return Texture[uv];
		}

		public enum Mode : byte
		{
			exact,
			height
		}
	}
}