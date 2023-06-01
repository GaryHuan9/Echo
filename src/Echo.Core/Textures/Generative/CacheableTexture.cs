using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Generative;

public abstract class CacheableTexture : Texture
{
	public Float2 Tiling { get; set; } = Float2.One;
	public Float2 Offset { get; set; } = Float2.Zero;

	public sealed override RGBA128 this[Float2 texcoord] => Sample(texcoord * Tiling + Offset);

	protected abstract RGBA128 Sample(Float2 position);

	//TODO: Implement cached sampling feature where texture chunks are baked as the samples are taken
}