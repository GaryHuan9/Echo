using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

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

	/// <summary>
	/// Evaluates this <see cref="InfiniteLight"/>.
	/// </summary>
	/// <param name="direction">The normalized world-space direction to evaluate at.</param>
	public abstract RGB128 Evaluate(in Float3 direction);

	/// <inheritdoc/>
	public abstract Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel);

	/// <inheritdoc/>
	public abstract float ProbabilityDensity(in GeometryPoint origin, in Float3 incident);
}