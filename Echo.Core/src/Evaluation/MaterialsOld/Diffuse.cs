﻿namespace Echo.Core.Evaluation.MaterialsOld
{
	public class Diffuse : MaterialOld
	{
		public override Float3 BidirectionalScatter(in TraceQuery query, ExtendedRandom random, out Float3 direction)
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