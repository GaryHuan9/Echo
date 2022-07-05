using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public interface IPreparedGeometry
{
	BoxBound BoxBound { get; }

	ConeBound ConeBound { get; }

	MaterialIndex Material { get; }

	float Area { get; }

	sealed float GetPower(PreparedSwatch swatch) => swatch[Material] is IEmissive emissive ? emissive.Power * Area : 0f;

	/// <summary>
	/// Samples this <see cref="IPreparedGeometry"/>.
	/// </summary>
	/// <param name="origin">The geometry-space point of whose perspective the sampling should be performed through.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value used to sample the result.</param>
	/// <returns>The <see cref="Probable{T}"/> of type <see cref="GeometryPoint"/> that was probabilistically sampled.</returns>
	Probable<GeometryPoint> Sample(in Float3 origin, Sample2D sample);

	/// <summary>
	/// Calculates the probability density function (pdf) value of sampling this <see cref="IPreparedGeometry"/> through <see cref="Sample"/>.
	/// </summary>
	/// <param name="origin">The geometry-space point of whose perspective the pdf should be calculated through.</param>
	/// <param name="incident">The sampled geometry-space unit direction that points from <paramref name="origin"/> towards this <see cref="IPreparedGeometry"/>.</param>
	/// <returns>The calculated pdf value over solid angles.</returns>
	float ProbabilityDensity(in Float3 origin, in Float3 incident);
}