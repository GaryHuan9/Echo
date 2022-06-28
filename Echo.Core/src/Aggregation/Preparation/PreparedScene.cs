using System;
using System.Collections.Immutable;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedScene : PreparedPack
{
	public PreparedScene(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
						 ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
						 SwatchExtractor swatchExtractor, ImmutableArray<InfiniteLight> infiniteLights)
		: base(geometrySources, lightSources, instances, in acceleratorCreator, swatchExtractor) => this.infiniteLights = infiniteLights;

	public readonly ImmutableArray<InfiniteLight> infiniteLights;

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		accelerator.Trace(ref query);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.travel)) return false;
		return accelerator.Occlude(ref query);
	}
}