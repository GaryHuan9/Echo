using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

/// <summary>
/// A source of photons; creates radiance and illuminates the <see cref="Scene"/>.
/// </summary>
public interface IPreparedLight
{
	/// <summary>
	/// Samples the contribution of this <see cref="IPreparedLight"/>.
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
	/// Returns the probability density function (pdf) value for incoming <paramref name="incident"/>
	/// direction pointing towards this <see cref="IPreparedAreaLight"/> to occur at <paramref name="point"/>.
	/// </summary>
	float ProbabilityDensity(in GeometryPoint point, in Float3 incident);
}