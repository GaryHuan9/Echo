using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public class Glossy : Material
	{
		public Float3 Albedo { get; set; }
		public float Smoothness { get; set; }

		public Texture AlbedoMap { get; set; } = Texture2D.white;
		public Texture SmoothnessMap { get; set; } = Texture2D.white;

		float randomRadius;

		public override void Press()
		{
			base.Press();

			AssertZeroOne(Smoothness);
			AssertZeroOne(Albedo);

			randomRadius = SmoothnessToRandomRadius(Smoothness);
		}

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			float radius = SampleTexture(SmoothnessMap, randomRadius, hit.texcoord);
			Float3 normal = (hit.normal + random.NextInSphere(radius)).Normalized;

			direction = hit.direction.Reflect(normal).Normalized;

			if (direction.Dot(hit.normal) < 0f) return Float3.zero; //Absorbed
			return SampleTexture(AlbedoMap, Albedo, hit.texcoord);
		}
	}
}