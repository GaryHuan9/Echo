using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public class Diffuse : Material
	{
		public Float3 Albedo { get; set; }
		public Texture AlbedoTexture { get; set; } = Texture2D.white;

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			direction = (hit.normal + random.NextOnSphere()).Normalized;
			return SampleTexture(AlbedoTexture, Albedo, hit.texcoord);
		}
	}
}