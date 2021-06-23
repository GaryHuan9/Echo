using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	public abstract class Generative : Texture
	{
		protected Generative() : base(Wrappers.unbound) { }

		public Float2 Tiling { get; set; } = Float2.one;
		public Float2 Offset { get; set; } = Float2.zero;

		protected sealed override Vector128<float> GetPixel(Float2 uv) => Sample(uv * Tiling + Offset);

		protected abstract Vector128<float> Sample(Float2 position);

		//TODO: Implement cached sampling feature where texture chunks are baked as the samples are taken
	}
}