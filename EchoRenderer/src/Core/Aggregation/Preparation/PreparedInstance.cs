using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Instancing;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation.Preparation;

public class PreparedInstance
{
	/// <summary>
	/// Creates a regular <see cref="PreparedInstance"/>.
	/// </summary>
	public PreparedInstance(ScenePreparer preparer, PackInstance instance, uint id) : this(preparer, instance.EntityPack, instance.Swatch, id)
	{
		forwardTransform = instance.WorldToLocal;
		inverseTransform = instance.LocalToWorld;

		Float3 scale = instance.Scale;

		inverseScale = scale.Average;
		forwardScale = 1f / inverseScale;

		if ((Float3)inverseScale != scale) throw new Exception($"{nameof(PackInstance)} does not support none uniform scaling! '{scale}'");
	}

	protected PreparedInstance(ScenePreparer preparer, EntityPack pack, MaterialSwatch swatch, uint id)
	{
		this.id = id;

		this.pack = preparer.GetPreparedPack(pack, out SwatchExtractor extractor, out NodeTokenArray tokenArray);

		this.swatch = extractor.Prepare(swatch);

		foreach (MaterialIndex index in this.swatch.EmissiveIndices)
		{
			// tokenArray[index]
		}
	}

	/// <summary>
	/// Computes the <see cref="AxisAlignedBoundingBox"/> of all contents inside this instance based on its parent's coordinate system.
	/// This <see cref="AxisAlignedBoundingBox"/> does not necessary enclose the root, only the enclosure of the content is guaranteed.
	/// NOTE: This property could be slow, so if performance issues arise try to memoize the result.
	/// </summary>
	public AxisAlignedBoundingBox AABB => pack.aggregator.GetTransformedAABB(inverseTransform);

	public readonly uint id;
	public readonly PreparedPack pack;
	public readonly PreparedSwatch swatch;

	public readonly Float4x4 forwardTransform = Float4x4.identity; //The parent to local transform
	public readonly Float4x4 inverseTransform = Float4x4.identity; //The local to parent transform

	readonly float forwardScale = 1f; //The parent to local scale multiplier
	readonly float inverseScale = 1f; //The local to parent scale multiplier

	readonly Piecewise1 powerDistribution;

	public float Power { get; }

	/// <summary>
	/// Processes <paramref name="query"/>.
	/// </summary>
	public void Trace(ref TraceQuery query)
	{
		var oldRay = query.ray;

		//Convert from parent space to local space
		TransformForward(ref query.ray);
		query.distance *= forwardScale;
		query.current.Push(this);

		//Gets intersection from aggregator in local space
		pack.aggregator.Trace(ref query);

		//Convert back to parent space
		query.distance *= inverseScale;
		query.ray = oldRay;
		query.current.Pop();
	}

	/// <summary>
	/// Processes <paramref name="query"/> and returns the result.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		var oldRay = query.ray;

		//Convert from parent space to local space
		TransformForward(ref query.ray);
		query.travel *= forwardScale;
		query.current.Push(this);

		//Gets intersection from aggregator in local space
		if (pack.aggregator.Occlude(ref query)) return true;

		//Convert back to parent space
		query.travel *= inverseScale;
		query.ray = oldRay;
		query.current.Pop();

		return false;
	}

	/// <summary>
	/// Returns the cost of tracing a <see cref="TraceQuery"/>.
	/// </summary>
	public int TraceCost(Ray ray, ref float distance)
	{
		//Forward transform distance to local space
		distance *= forwardScale;

		//Gets intersection cost, calculation done in local space
		TransformForward(ref ray);

		int cost = pack.aggregator.TraceCost(ray, ref distance);

		//Restore distance back to parent space
		distance *= inverseScale;
		return cost;
	}

	/// <summary>
	/// Transforms <paramref name="ray"/> from parent to local space.
	/// </summary>
	void TransformForward(ref Ray ray)
	{
		Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
		Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

		ray = new Ray(origin, direction * inverseScale);
	}
}