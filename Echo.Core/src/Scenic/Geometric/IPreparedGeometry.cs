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
	AxisAlignedBoundingBox AABB { get; }

	ConeBounds ConeBounds { get; }

	float GetPower(PreparedSwatch swatch);
}

public interface IPreparedPureGeometry : IPreparedGeometry
{
	MaterialIndex Material { get; }

	float Area { get; }

	/// <summary>
	/// Samples this <see cref="IPreparedPureGeometry"/>.
	/// </summary>
	/// <param name="origin">The geometry-space point of whose perspective the sampling should be performed through.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value used to sample the result.</param>
	/// <returns>The <see cref="Probable{T}"/> of type <see cref="GeometryPoint"/> that was probabilistically sampled.</returns>
	Probable<GeometryPoint> Sample(in Float3 origin, Sample2D sample);

	/// <summary>
	/// Calculates the probability density function (pdf) value of sampling this <see cref="IPreparedPureGeometry"/> through <see cref="Sample"/>.
	/// </summary>
	/// <param name="origin">The geometry-space point of whose perspective the pdf should be calculated through.</param>
	/// <param name="incident">The sampled geometry-space unit direction that points from <paramref name="origin"/> towards this <see cref="IPreparedPureGeometry"/>.</param>
	/// <returns>The calculated pdf value over solid angles.</returns>
	float ProbabilityDensity(in Float3 origin, in Float3 incident);

	float IPreparedGeometry.GetPower(PreparedSwatch swatch) => swatch[Material] is IEmissive emissive ? emissive.Power * Area : 0f;
}