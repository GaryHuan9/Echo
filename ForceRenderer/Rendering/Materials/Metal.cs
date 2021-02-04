using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public class Metal : Material
	{
		public Float3 Albedo { get; set; }
		public float Smoothness { get; set; }

		public Texture AlbedoTexture { get; set; } = Texture2D.white;
		public Texture SmoothnessTexture { get; set; } = Texture2D.white;

		float fuzziness;

		public override void Press()
		{
			base.Press();
			fuzziness = MathF.Pow(1f - Smoothness, 1.8f);
		}

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			Float3 fuzzy = random.NextInSphere(SampleTexture(SmoothnessTexture, fuzziness, hit.texcoord));
			direction = (hit.direction.Reflect(hit.normal) + fuzzy).Normalized;

			if (direction.Dot(hit.normal) < 0f) return Float3.zero; //Absorbed
			return SampleTexture(AlbedoTexture, Albedo, hit.texcoord);
		}
	}
}