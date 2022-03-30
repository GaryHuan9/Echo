using System.Runtime.Intrinsics;
using CodeHelpers.Packed;

namespace EchoRenderer.Core.Texturing.Generative;

public abstract class CacheableTexture : Texture
{
	protected CacheableTexture() : base(Wrappers.unbound) { }

	public Float2 Tiling { get; set; } = Float2.One;
	public Float2 Offset { get; set; } = Float2.Zero;

	protected sealed override Vector128<float> Evaluate(Float2 uv) => Sample(uv * Tiling + Offset);

	protected abstract Vector128<float> Sample(Float2 position);

	//TODO: Implement cached sampling feature where texture chunks are baked as the samples are taken
}