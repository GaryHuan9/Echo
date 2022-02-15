using System;
using CodeHelpers;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Acceleration;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Scenic.Instancing;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation;

public record AggregatorProfile : IProfile
{
	/// <summary>
	/// Explicitly indicate the type of <see cref="Aggregator"/> to use. This can be left as null for automatic selection.
	/// </summary>
	public Type AggregatorType { get; init; }

	/// <summary>
	/// If this is true, <see cref="LinearAggregator"/> can be used for <see cref="PreparedPack"/> that contains <see cref="PackInstance"/>.
	/// NOTE: normally this should be false because we should avoid actually intersecting with an <see cref="PackInstance"/> as much as possible.
	/// </summary>
	public bool LinearForInstances { get; init; } = false;

	//NOTE: These thresholds have not been tested yet, they are just pulled from the back of my head
	const ulong ThresholdBVH = 32;
	const ulong ThresholdQuadBVH = 512;

	/// <inheritdoc/>
	public void Validate()
	{
		if (AggregatorType?.IsSubclassOf(typeof(Aggregator)) == false) throw ExceptionHelper.Invalid(nameof(AggregatorType), AggregatorType, $"is not of type {nameof(Aggregator)}");
	}

	public Aggregator CreateAggregator(PreparedPack pack, ReadOnlyMemory<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens)
	{
		if (AggregatorType != null) return CreateExplicit(AggregatorType, pack, aabbs, tokens);

		ulong total = pack.counts.Total;

		//If there is enough geometry
		if (total > 1)
		{
			if (total >= ThresholdQuadBVH) return new QuadBoundingVolumeHierarchy(pack, aabbs, tokens);
			if (total >= ThresholdBVH) return new BoundingVolumeHierarchy(pack, aabbs, tokens);

			//If there is an instance in the pack and our configuration disallows a linear aggregator for to store instances
			if (!LinearForInstances && pack.counts.instance > 0) return new BoundingVolumeHierarchy(pack, aabbs, tokens);
		}

		//Base case defaults to linear aggregator
		return new LinearAggregator(pack, aabbs, tokens);
	}

	static Aggregator CreateExplicit(Type type, PreparedPack pack, ReadOnlyMemory<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens)
	{
		if (type == typeof(LinearAggregator)) return new LinearAggregator(pack, aabbs, tokens);
		if (type == typeof(BoundingVolumeHierarchy)) return new BoundingVolumeHierarchy(pack, aabbs, tokens);
		if (type == typeof(QuadBoundingVolumeHierarchy)) return new QuadBoundingVolumeHierarchy(pack, aabbs, tokens);

		throw ExceptionHelper.Invalid(nameof(type), type, InvalidType.unexpected);
	}
}