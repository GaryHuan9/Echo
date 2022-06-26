using System;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Lighting;

namespace Echo.Core.Aggregation.Selection;

public abstract class LightPicker
{
	public abstract Probable<EntityToken> Pick(Sample1D sample);

	public abstract float ProbabilityDensity(EntityToken token);

	public static LightPicker Create() => throw new NotImplementedException();

	public static LightPicker Create(LightCollection lights, GeometryCollection geometries)
	{
		
		
		throw new NotImplementedException();
	}
}