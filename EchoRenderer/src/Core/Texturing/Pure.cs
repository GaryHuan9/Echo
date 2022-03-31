using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Texturing.Directional;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// A readonly pure-color <see cref="Texture"/> and <see cref="IDirectionalTexture"/>.
/// </summary>
public class Pure : Texture, IDirectionalTexture
{
	public Pure(in RGBA128 color) : base(Wrappers.unbound) => this.color = color;

	readonly RGBA128 color;

	public override Int2 DiscreteResolution => Int2.One;

	RGBA128 IDirectionalTexture.Average => color;

	protected override RGBA128 Evaluate(Float2 uv) => color;

	RGBA128 IDirectionalTexture.Evaluate(in Float3 direction) => color;

	public static explicit operator Pure(in RGBA128 color) => new(color);
}