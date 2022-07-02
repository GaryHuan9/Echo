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
	public abstract ConeBounds ConeBounds { get; }

	public abstract float Power { get; }

	public abstract AxisAlignedBoundingBox GetTransformedBounds(in Float4x4 transform);

	public virtual Probable<EntityToken> Pick(Sample1D sample) => Pick(ref sample);

	public abstract Probable<EntityToken> Pick(ref Sample1D sample);

	public abstract float ProbabilityMass(EntityToken token);

	public static LightPicker Create(LightCollection lights)
	{
		View<Tokenized<LightBounds>> boundsView = lights.CreateBoundsView();

		throw new NotImplementedException();
	}
}