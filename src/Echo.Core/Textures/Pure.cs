using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Textures;

/// <summary>
/// A readonly pure-color <see cref="Texture"/> and <see cref="IDirectionalTexture"/>.
/// </summary>
[EchoSourceUsable]
public sealed class Pure : Texture, IDirectionalTexture
{
	[EchoSourceUsable]
	public Pure(RGBA128 color)
	{
		this.color = (RGB128)color;
		rgba = color;
	}

	readonly RGB128 color;
	readonly RGBA128 rgba;

	public override Int2 DiscreteResolution => Int2.One;

	RGB128 IDirectionalTexture.Average => color;

	public override RGBA128 this[Float2 texcoord] => rgba;

	RGB128 IDirectionalTexture.Evaluate(Float3 incident) => color;

	public static explicit operator Pure(RGBA128 color) => new(color);
	public static readonly Pure white = new(RGBA128.White);
	public static readonly Pure black = new(RGBA128.Black);
	public static readonly Pure clear = new(RGBA128.Zero);
	public static readonly Pure normal = new(new RGBA128(0.5f, 0.5f, 1f));
}