using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures;

/// <summary>
/// A wrapper <see cref="Texture"/> to alter the result of another <see cref="Texture"/>.
/// </summary>
[EchoSourceUsable]
public class AdaptorTexture : Texture
{
	[EchoSourceUsable]
	public AdaptorTexture(Texture texture) => this.texture = texture;

	readonly Texture texture;

	bool hasTransform;
	bool hasSwizzle;

	Vector128<byte> swizzler;

	Float4 _scale = Float4.One;
	Float4 _shift = Float4.Zero;

	/// <summary>
	/// The value to multiply the original <see cref="Texture"/> by.
	/// </summary>
	/// <remarks>This is applied before <see cref="Shift"/> and the swizzle.</remarks>
	[EchoSourceUsable]
	public Float4 Scale
	{
		get => _scale;
		set
		{
			hasTransform = true;
			_scale = value;
		}
	}

	/// <summary>
	/// The value to add to the original <see cref="Texture"/>.
	/// </summary>
	/// <remarks>This is applied after <see cref="Scale"/> and before the swizzle.</remarks>
	[EchoSourceUsable]
	public Float4 Shift
	{
		get => _shift;
		set
		{
			hasTransform = true;
			_shift = value;
		}
	}

	public override RGBA128 this[Float2 texcoord]
	{
		get
		{
			Vector128<float> value = ((Float4)texture[texcoord]).v;

			if (hasTransform)
			{
				if (!Fma.IsSupported)
				{
					value = Sse.Multiply(value, Scale.v);
					value = Sse.Add(value, Shift.v);
				}
				else value = Fma.MultiplyAdd(value, Scale.v, Shift.v);
			}

			if (hasSwizzle) value = Ssse3.Shuffle(value.AsByte(), swizzler).AsSingle();

			return (RGBA128)new Float4(value);
		}
	}

	/// <summary>
	/// Manipulates the order of the four channels of the evaluated color. 
	/// </summary>
	/// <param name="pattern">A four-character string indicating where each output channel should get its value.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="pattern"/> is invalid.</exception>
	/// <remarks>For example, if the original color is (1, 2, 3, 4) and the <paramref name="pattern"/> is "YXWW",
	/// then the resulting color is (2, 1, 4, 4). This is applied before <see cref="Scale"/> and <see cref="Shift"/>.</remarks>
	[EchoSourceUsable]
	public unsafe void SetSwizzle(string pattern)
	{
		const int Width = 4;
		const int Bytes = 4;

		if (pattern.Length != Width) throw new ArgumentOutOfRangeException(nameof(pattern));

		hasSwizzle = true;

		byte* result = stackalloc byte[Bytes * Width];

		for (int i = 0; i < Width; i++)
		{
			int index = char.ToUpper(pattern[i]) switch
			{
				'X' => 0, 'Y' => 1, 'Z' => 2, 'W' => 3,
				_ => throw new ArgumentOutOfRangeException(nameof(pattern))
			};

			for (int j = 0; j < Bytes; j++)
			{
				result[i * Bytes + j] = (byte)(index * Bytes + j);
			}
		}

		swizzler = Sse2.LoadVector128(result);
	}
}