using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// The source of color; creates radiance and illuminates a <see cref="Scene"/>.
/// </summary>
public interface IPreparedLight
{
	/// <summary>
	/// A <see cref="BoxBound"/> that bounds all of the position
	/// from which this <see cref="IPreparedLight"/> can emit.
	/// </summary>
	public BoxBound BoxBound { get; }

	/// <summary>
	/// A <see cref="ConeBound"/> that bounds all of the direction from which
	/// this <see cref="IPreparedLight"/> can emit towards, including the falloff.
	/// </summary>
	public ConeBound ConeBound { get; }

	/// <summary>
	/// The total emissive power of this <see cref="IPreparedLight"/>.
	/// </summary>
	/// <remarks>This value can be approximated.</remarks>
	public float Power { get; }

	/// <summary>
	/// Samples the contribution of this <see cref="IPreparedLight"/>.
	/// </summary>
	/// <param name="origin">The point towards which we are sampling the contribution of this <see cref="IPreparedLight"/> to.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value to use for this sampling.</param>
	/// <param name="incident">The incident direction pointing from <paramref name="origin"/> towards this <see cref="IPreparedLight"/>.</param>
	/// <param name="travel">The distance to travel in light-space from <paramref name="origin"/> to this <see cref="IPreparedLight"/>.</param>
	/// <returns>The sampled <see cref="Probable{T}"/> of type <see cref="RGB128"/>.</returns>
	public Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel);

	/// <summary>
	/// Calculates the probability density function (pdf) value of sampling this <see cref="IPreparedLight"/>.
	/// </summary>
	/// <param name="origin">The point towards which this <see cref="IPreparedLight"/> should be contributing energy to.</param>
	/// <param name="incident">An incoming direction that points from <paramref name="origin"/> towards this <see cref="IPreparedLight"/>.</param>
	/// <returns>The calculated pdf value for the <paramref name="incident"/> direction occur at <paramref name="origin"/>.</returns>
	/// <remarks>For <see cref="IPreparedLight"/> that is a singularity (ie. from a single point), this simply returns 1.</remarks>
	public float ProbabilityDensity(in GeometryPoint origin, Float3 incident);
}