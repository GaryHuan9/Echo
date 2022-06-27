using System;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Selection;

public abstract class LightPicker
{
	public abstract float Power { get; }

	public abstract ConeBounds GetTransformedBounds(in Float4x4 transform);

	public abstract Probable<EntityToken> Pick(Sample1D sample);

	public abstract float ProbabilityDensity(EntityToken token);

	public static LightPicker Create(LightCollection lights, PreparedSwatch emissiveSwatch)
	{
		View<Tokenized<LightBounds>> boundsView = lights.CreateBoundsView(emissiveSwatch);

		throw new NotImplementedException();
	}
}