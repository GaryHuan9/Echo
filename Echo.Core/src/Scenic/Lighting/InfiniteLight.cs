using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

/// <summary>
/// An <see cref="LightEntity"/> that is infinitely far away from the scene.
/// </summary>
/// <remarks>All <see cref="InfiniteLight"/> must have an area; delta <see cref="InfiniteLight"/> is not supported.</remarks>
public abstract class InfiniteLight : LightEntity, IPreparedAreaLight
{
	public abstract float Power { get; }

	/// <summary>
	/// Invoked before rendering; after geometry and other lights are prepared.
	/// Can be used to initialize this infinite light to prepare it for rendering.
	/// </summary>
	public virtual void Prepare(PreparedScene scene) { }

	/// <inheritdoc/>
	public abstract Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel);

	/// <inheritdoc/>
	public abstract float ProbabilityDensity(in GeometryPoint origin, in Float3 incident);
}