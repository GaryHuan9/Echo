using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public class Glossy : MaterialOld
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

		public override Float3 BidirectionalScatter(in TraceQuery query, ExtendedRandom random, out Float3 direction)
		{
			if (CullBackface(query) || AlphaTest(query, out Float3 color))
			{
				direction = query.ray.direction;
				return Float3.one;
			}

			float radius = SampleTexture(SmoothnessMap, randomRadius, query.shading.texcoord);
			Float3 normal = (query.shading.normal + random.NextInSphere(radius)).Normalized;

			direction = query.ray.direction.Reflect(normal).Normalized;
			if (direction.Dot(query.shading.normal) < 0f) color = Float3.zero; //Absorbed

			return color;
		}
	}
}