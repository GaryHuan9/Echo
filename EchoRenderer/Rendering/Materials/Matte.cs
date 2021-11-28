using System;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Materials
{
	public class Matte : MaterialNew
	{
		public override void Scatter(ref HitQuery query, Arena arena, TransportMode mode)
		{
			var bsdf = arena.allocator.New<BidirectionalScatteringDistributionFunctions>();

			bsdf.Add();
		}
	}
}