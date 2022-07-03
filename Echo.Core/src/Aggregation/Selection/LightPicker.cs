using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;

namespace Echo.Core.Aggregation.Selection;

public abstract class LightPicker
{
	public abstract ConeBounds ConeBounds { get; }

	public abstract float Power { get; }

	public abstract AxisAlignedBoundingBox GetTransformedBounds(in Float4x4 transform);

	public virtual Probable<EntityToken> Pick(in GeometryPoint origin, Sample1D sample) => Pick(origin, ref sample);

	public abstract Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample);

	public abstract float ProbabilityMass(in GeometryPoint origin,EntityToken token);

	public static LightPicker Create(LightCollection lights)
	{
		View<Tokenized<LightBounds>> boundsView = lights.CreateBoundsView();
		return new LightBoundingVolumeHierarchy(boundsView);
	}
}