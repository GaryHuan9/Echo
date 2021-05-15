using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public class Glossy : Material
	{
		public float Smoothness { get; set; }
		public Texture SmoothnessMap { get; set; } = Texture.white;

		float randomRadius;

		public override void Press()
		{
			base.Press();
			AssertZeroOne(Smoothness);

			randomRadius = SmoothnessToRandomRadius(Smoothness);
		}

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			if (CullBackface(hit) || AlphaTest(hit, out Float3 color))
			{
				direction = hit.direction;
				return Float3.one;
			}

			float radius = SampleTexture(SmoothnessMap, randomRadius, hit.texcoord);
			Float3 normal = (hit.normal + random.NextInSphere(radius)).Normalized;

			direction = hit.direction.Reflect(normal).Normalized;
			if (direction.Dot(hit.normal) < 0f) color = Float3.zero; //Absorbed

			return color;
		}
	}
}