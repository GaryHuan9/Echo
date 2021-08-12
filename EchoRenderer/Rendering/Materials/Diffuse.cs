using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Materials
{
	public class Diffuse : Material
	{
		public override Float3 BidirectionalScatter(in HitQuery query, ExtendedRandom random, out Float3 direction)
		{
			if (CullBackface(query) || AlphaTest(query, out Float3 color))
			{
				direction = query.ray.direction;
				return Float3.one;
			}

			direction = (query.shading.normal + random.NextOnSphere()).Normalized;
			return color;
		}
	}
}