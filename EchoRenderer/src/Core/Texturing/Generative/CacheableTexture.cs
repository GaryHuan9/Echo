using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Texturing.Generative;

public abstract class CacheableTexture : Texture
{
	protected CacheableTexture() : base(Wrappers.unbound) { }

	public Float2 Tiling { get; set; } = Float2.One;
	public Float2 Offset { get; set; } = Float2.Zero;

	protected sealed override RGBA128 Evaluate(Float2 uv) => Sample(uv * Tiling + Offset);

	protected abstract RGBA128 Sample(Float2 position);

	//TODO: Implement cached sampling feature where texture chunks are baked as the samples are taken
}