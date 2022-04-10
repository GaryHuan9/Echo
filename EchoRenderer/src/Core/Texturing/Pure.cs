using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Texturing.Directional;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// A readonly pure-color <see cref="Texture"/> and <see cref="IDirectionalTexture"/>.
/// </summary>
public class Pure : Texture, IDirectionalTexture
{
	public Pure(in RGBA128 color)
	{
		this.color = (RGB128)color;
		rgba = color;
	}

	readonly RGB128 color;
	readonly RGBA128 rgba;

	public override Int2 DiscreteResolution => Int2.One;

	RGB128 IDirectionalTexture.Average => color;

	public void Prepare() => Tint = Tint.Identity;

	protected override RGBA128 Evaluate(Float2 uv) => rgba;

	RGB128 IDirectionalTexture.Evaluate(in Float3 direction) => color;
}