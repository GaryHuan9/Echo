using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

/// <summary>
/// The source of color; creates radiance and illuminates the <see cref="Scene"/>.
/// </summary>
public interface IPreparedLight
{
	/// <summary>
	/// Samples the contribution of an <see cref="IPreparedLight"/>.
	/// </summary>
	/// <param name="point">The point towards which we are sampling the contribution of this <see cref="IPreparedLight"/> to.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value to use for this sampling.</param>
	/// <param name="incident">The incident direction pointing from <paramref name="point"/> towards this <see cref="IPreparedLight"/>.</param>
	/// <param name="travel">The distance to travel in light-space from <paramref name="point"/> to this <see cref="IPreparedLight"/>.</param>
	/// <returns>The sampled <see cref="Probable{T}"/> of type <see cref="RGB128"/>.</returns>
	Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel);
}

/// <summary>
/// An <see cref="IPreparedLight"/> that has area, which means that is it not a singularity.
/// </summary>
public interface IPreparedAreaLight : ILight
{
	/// <summary>
	/// Calculates the probability density function (pdf) value of sampling an <see cref="IPreparedAreaLight"/>.
	/// </summary>
	/// <param name="point">The point towards which this <see cref="IPreparedAreaLight"/> should be contributing energy to.</param>
	/// <param name="incident">An incoming direction that points from <paramref name="point"/> towards this <see cref="IPreparedAreaLight"/>.</param>
	/// <returns>The calculated pdf value for the <paramref name="incident"/> direction occur at <paramref name="point"/>.</returns>
	float ProbabilityDensity(in GeometryPoint point, in Float3 incident);
}