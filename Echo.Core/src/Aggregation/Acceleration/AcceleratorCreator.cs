using System;
using CodeHelpers;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Aggregation.Acceleration;

public readonly record struct AcceleratorCreator
{
	readonly Type _acceleratorType;

	/// <summary>
	/// Explicitly indicate the type of <see cref="Accelerator"/> to use. This can be left as null for automatic selection.
	/// </summary>
	public Type AcceleratorType
	{
		get => _acceleratorType;
		init
		{
			if (value?.IsSubclassOf(typeof(Accelerator)) != false) _acceleratorType = value;
			else throw ExceptionHelper.Invalid(nameof(value), value, $"is not of type {nameof(Accelerator)}");
		}
	}

	/// <summary>
	/// If this is true, <see cref="LinearAccelerator"/> can be used for <see cref="PreparedPack"/> that contains <see cref="PackInstance"/>.
	/// NOTE: normally this should be false because we should avoid actually intersecting with an <see cref="PackInstance"/> as much as possible.
	/// </summary>
	public bool LinearForInstances { get; init; }

	//NOTE: These thresholds are not scientific, they are just pulled from the back of my head
	const ulong ThresholdBVH = 32;
	const ulong ThresholdQuadBVH = 512;

	public Accelerator Create(GeometryCollection geometries)
	{
		if (AcceleratorType != null) return CreateExplicit(AcceleratorType, geometries);

		ulong total = geometries.counts.Total;

		//If there is enough geometry
		if (total > 1)
		{
			if (total >= ThresholdQuadBVH) return CreateQBVH(geometries);
			if (total >= ThresholdBVH) return CreateBVH(geometries);

			//If there is an instance in the pack and our configuration disallows a linear aggregator to store instances
			if (!LinearForInstances && geometries.counts.instance > 0) return CreateBVH(geometries);
		}

		//Base case defaults to linear aggregator
		return CreateLinear(geometries);

		static Accelerator CreateExplicit(Type type, GeometryCollection geometries)
		{
			if (type == typeof(LinearAccelerator)) return CreateLinear(geometries);
			if (type == typeof(BoundingVolumeHierarchy)) return CreateBVH(geometries);
			if (type == typeof(QuadBoundingVolumeHierarchy)) return CreateQBVH(geometries);

			throw ExceptionHelper.Invalid(nameof(type), type, InvalidType.unexpected);
		}
	}

	static Accelerator CreateLinear(GeometryCollection geometries) => new LinearAccelerator(geometries, geometries.CreateBounds());

	static Accelerator CreateBVH(GeometryCollection geometries)
	{
		var bounds = geometries.CreateBounds();
		var builder = new SweepBuilder(bounds);

		return new BoundingVolumeHierarchy(geometries, builder.Build());
	}

	static Accelerator CreateQBVH(GeometryCollection geometries)
	{
		var bounds = geometries.CreateBounds();
		var builder = new SweepBuilder(bounds);

		return new QuadBoundingVolumeHierarchy(geometries, builder.Build());
	}
}