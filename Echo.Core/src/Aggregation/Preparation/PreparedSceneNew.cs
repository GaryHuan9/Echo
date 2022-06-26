using System;
using System.Collections.Immutable;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public class PreparedSceneNew : PreparedPack
{
	public PreparedSceneNew(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
							ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
							SwatchExtractor swatchExtractor)
		: base(geometrySources, lightSources, instances, in acceleratorCreator, swatchExtractor)
	{
		
	}
}