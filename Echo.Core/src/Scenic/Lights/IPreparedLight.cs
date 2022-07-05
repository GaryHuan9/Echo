using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// The source of color; creates radiance and illuminates the <see cref="Scene"/>.
/// </summary>
public interface IPreparedLight
{
	/// <summary>
	/// Samples the contribution of an <see cref="IPreparedLight"/>.
	/// </summary>
	/// <param name="origin">The point towards which we are sampling the contribution of this <see cref="IPreparedLight"/> to.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value to use for this sampling.</param>
	/// <param name="incident">The incident direction pointing from <paramref name="origin"/> towards this <see cref="IPreparedLight"/>.</param>
	/// <param name="travel">The distance to travel in light-space from <paramref name="origin"/> to this <see cref="IPreparedLight"/>.</param>
	/// <returns>The sampled <see cref="Probable{T}"/> of type <see cref="RGB128"/>.</returns>
	Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel);
}

/// <summary>
/// An <see cref="IPreparedLight"/> that has area, which means that is it not a singularity.
/// </summary>
public interface IPreparedAreaLight : IPreparedLight
{
	/// <summary>
	/// Calculates the probability density function (pdf) value of sampling an <see cref="IPreparedAreaLight"/>.
	/// </summary>
	/// <param name="origin">The point towards which this <see cref="IPreparedAreaLight"/> should be contributing energy to.</param>
	/// <param name="incident">An incoming direction that points from <paramref name="origin"/> towards this <see cref="IPreparedAreaLight"/>.</param>
	/// <returns>The calculated pdf value for the <paramref name="incident"/> direction occur at <paramref name="origin"/>.</returns>
	float ProbabilityDensity(in GeometryPoint origin, in Float3 incident);
}