using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public class Metal : Material
	{
		public Float3 Albedo { get; set; }
		public float Smoothness { get; set; }

		public Texture AlbedoMap { get; set; } = Texture2D.white;
		public Texture SmoothnessMap { get; set; } = Texture2D.white;

		float fuzziness;

		public override void Press()
		{
			base.Press();

			AssertZeroOne(Smoothness);
			AssertZeroOne(Albedo);

			fuzziness = MathF.Pow(1f - Smoothness, 1.8f);
		}

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			Float3 fuzzy = random.NextInSphere(SampleTexture(SmoothnessMap, fuzziness, hit.texcoord));
			direction = (hit.direction.Reflect(hit.normal) + fuzzy).Normalized;

			if (direction.Dot(hit.normal) < 0f) return Float3.zero; //Absorbed
			return SampleTexture(AlbedoMap, Albedo, hit.texcoord);
		}
	}
}