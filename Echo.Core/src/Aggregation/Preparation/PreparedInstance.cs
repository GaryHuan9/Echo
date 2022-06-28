using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public readonly struct PreparedInstance : IPreparedGeometry
{
	public PreparedInstance(PreparedPack pack, PreparedSwatch swatch, in Float4x4 inverseTransform)
	{
		this.pack = pack;
		this.swatch = swatch;

		this.inverseTransform = inverseTransform;
		forwardTransform = inverseTransform.Inversed;

		inverseScale = inverseTransform.GetRow(0).XYZ_.Magnitude;
		forwardScale = 1f / inverseScale;
	}

	public readonly PreparedPack pack;
	public readonly PreparedSwatch swatch;

	public readonly Float4x4 forwardTransform; //The parent to local transform
	public readonly Float4x4 inverseTransform; //The local to parent transform

	readonly float forwardScale = 1f; //The parent to local scale multiplier
	readonly float inverseScale = 1f; //The local to parent scale multiplier

	public AxisAlignedBoundingBox AABB => pack.accelerator.GetTransformedBounds(inverseTransform);

	public ConeBounds ConeBounds => pack.lightPicker.GetTransformedBounds(inverseTransform);

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
		query.distance *= inverseScale;
		query.ray = oldRay;
	}

	/// <summary>
	/// Processes a <see cref="OccludeQuery"/> through the underlying <see cref="Accelerator"/>.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		var oldRay = query.ray;

		//Convert from parent-space to local-space
		TransformForward(ref query.ray);
		query.travel *= forwardScale;

		//Gets intersection from accelerator in local-space
		if (pack.accelerator.Occlude(ref query)) return true;

		//Convert back to parent-space
		query.travel *= inverseScale;
		query.ray = oldRay;

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
	/// Retrieves a <see cref="Material"/> from this <see cref="PreparedInstance"/>.
	/// </summary>
	/// <param name="index">The <see cref="MaterialIndex"/> pointing at the target <see cref="Material"/>.</param>
	/// <returns>The <see cref="Material"/> that was found.</returns>
	public Material GetMaterial(MaterialIndex index) => swatch[index];

	float IPreparedGeometry.GetPower(PreparedSwatch _) => pack.lightPicker.Power * inverseScale * inverseScale;

	/// <summary>
	/// Picks an emissive geometry from this <see cref="PreparedInstance"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample1D"/> value used to select the target emissive geometry.</param>
	/// <param name="instance">Outputs the <see cref="PreparedInstance"/> that immediately holds this geometry.</param>
	/// <returns>The picked <see cref="Probable{T}"/> of type <see cref="TokenHierarchy"/> that represents our emissive geometry.</returns>
	/// <remarks>If <see cref="Power"/> is not positive, the behavior of this method is undefined.</remarks>
	public Probable<TokenHierarchy> Pick(Sample1D sample, out PreparedInstance instance)
	{
		Assert.IsTrue(FastMath.Positive(Power));
		var geometryToken = new TokenHierarchy();

		instance = this;
		float pdf = 1f;

		do
		{
			// ReSharper disable once LocalVariableHidesMember
			(EntityToken token, float tokenPdf) = instance.powerDistribution.Pick(ref sample);

			Assert.IsTrue(token.Type.IsGeometry());

			if (token.Type.IsRawGeometry()) geometryToken.TopToken = token;
			else geometryToken.Push(instance = instance.pack.GetInstance(token));

			pdf *= tokenPdf;
		}
		while (geometryToken.TopToken == default);

		return (geometryToken, pdf);
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