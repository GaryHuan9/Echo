using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public readonly struct PreparedInstance
{
	public PreparedInstance(PreparedPack pack, PreparedSwatch swatch, in Float4x4 inverseTransform)
	{
		this.pack = pack;
		this.swatch = swatch;

		this.inverseTransform = inverseTransform;
		forwardTransform = inverseTransform.Inverse;

		inverseScale = inverseTransform.GetRow(0).XYZ_.Magnitude;
		forwardScale = 1f / inverseScale;
	}

	public readonly PreparedPack pack;
	public readonly PreparedSwatch swatch;

	public readonly Float4x4 forwardTransform; //The parent to local transform
	public readonly Float4x4 inverseTransform; //The local to parent transform

	readonly float forwardScale = 1f; //The parent to local scale multiplier
	readonly float inverseScale = 1f; //The local to parent scale multiplier

	public BoxBound BoxBound => pack.accelerator.GetTransformedBound(inverseTransform);

	public LightBound LightBound => new
	(
		pack.lightPicker.GetTransformedBounds(inverseTransform),
		inverseTransform * pack.lightPicker.ConeBound,
		Power
	);

	public float Power => pack.lightPicker.Power * inverseScale * inverseScale;

	/// <summary>
	/// Processes a <see cref="TraceQuery"/> through the underlying <see cref="Accelerator"/>.
	/// </summary>
	public void Trace(ref TraceQuery query)
	{
		var oldRay = query.ray;

		//Convert from parent-space to local-space
		TransformForward(ref query.ray);
		query.distance *= forwardScale;

		//Gets intersection from accelerator in local-space
		pack.accelerator.Trace(ref query);

		//Convert back to parent-space
		query.ray = oldRay;
		query.distance *= inverseScale;
	}

	/// <summary>
	/// Processes a <see cref="OccludeQuery"/> through the underlying <see cref="Accelerator"/>.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		var oldRay = query.ray;
		var oldTravel = query.travel;

		//Convert from parent-space to local-space
		TransformForward(ref query.ray);
		query.travel *= forwardScale;

		//Gets intersection from accelerator in local-space
		if (pack.accelerator.Occlude(ref query)) return true;

		//Convert back to parent-space
		query.ray = oldRay;
		query.travel = oldTravel;

		return false;
	}

	/// <summary>
	/// Returns the cost of tracing a <see cref="TraceQuery"/>.
	/// </summary>
	public uint TraceCost(Ray ray, ref float distance)
	{
		//Forward transform distance to local-space
		distance *= forwardScale;

		//Gets intersection cost, calculation done in local-space
		TransformForward(ref ray);

		uint cost = pack.accelerator.TraceCost(ray, ref distance);

		//Restore distance back to parent-space
		distance *= inverseScale;
		return cost;
	}

	/// <summary>
	/// Transforms <paramref name="ray"/> from parent to local-space.
	/// </summary>
	void TransformForward(ref Ray ray)
	{
		Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
		Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

		ray = new Ray(origin, direction * inverseScale);
	}
}