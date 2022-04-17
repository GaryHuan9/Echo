using CodeHelpers.Packed;
using EchoRenderer.Core.Textures.Colors;

namespace EchoRenderer.Core.Textures;

/// <summary>
/// A custom linear transform that can be applied to a color.
/// </summary>
public readonly struct Tint
{
	Tint(in Float4 scale, in Float4 offset)
	{
		this.scale = scale;
		this.offset = offset;
	}

	readonly Float4 scale;
	readonly Float4 offset;

	public static Tint Identity => new(RGBA128.White, RGBA128.Zero);

	public RGBA128 Apply(in RGBA128 color)
	{
		//OPTIMIZE
		return (RGBA128)(color * scale + offset);
		// return PackedMath.FMA(color, scale, offset);
	}

	public static Tint Scale(in RGBA128 value) => new(value, RGBA128.Zero);
	public static Tint Offset(in RGBA128 value) => new(RGBA128.White, value);

	public static Tint Inverse(in RGBA128 value) => new(-(Float4)value, value);
	public static Tint Inverse() => Inverse(RGBA128.White);
}