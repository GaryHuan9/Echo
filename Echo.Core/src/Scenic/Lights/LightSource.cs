using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// An <see cref="ILight"/> <see cref="Entity"/> explicitly contained in the <see cref="Scene"/>.
/// Note that light sources created this way must be contained in the root <see cref="EntityPack"/>.
/// </summary>
public abstract class LightSource : Entity, ILight
{
	/// <summary>
	/// The main color and intensity of this <see cref="LightSource"/>.
	/// </summary>
	public RGB128 Intensity { get; set; } = RGB128.White;

	/// <summary>
	/// The approximated total emitted power of this <see cref="LightSource"/>.
	/// </summary>
	public abstract float Power { get; }

	/// <summary>
	/// Invoked before rendering; after geometry and materials are prepared.
	/// Can be used to initialize this light to prepare it for rendering.
	/// </summary>
	public virtual void Prepare(PreparedScene scene) { }

	/// <inheritdoc/>
	public abstract Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel);
}

/// <summary>
/// An <see cref="IAreaLight"/> <see cref="Entity"/> inheriting <see cref="LightSource"/>.
/// Can be used to define light sources that have area and are not singularities (delta).
/// </summary>
public abstract class AreaLightSource : LightSource, IAreaLight
{
	/// <inheritdoc/>
	public abstract float ProbabilityDensity(in GeometryPoint point, in Float3 incident);
}

/// <summary>
/// A source of photons; creates radiance and illuminates the <see cref="Scene"/>.
/// NOTE: the entirety of the light system (ie. parameters etc.) is in world-space.
/// </summary>
public interface ILight
{
	/// <summary>
	/// Samples the contribution of this <see cref="ILight"/> to <paramref name="point"/>. The <paramref name="incident"/> direction that
	/// points from <paramref name="point"/> towards this <see cref="ILight"/>, the probability density function <paramref name="pdf"/> value,
	/// and the <paramref name="travel"/> distance in light-space from <paramref name="point"/> to this <see cref="ILight"/> are outputted.
	/// </summary>
	Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel);
}

/// <summary>
/// An <see cref="ILight"/> that has area, which means that is it not a singularity.
/// </summary>
public interface IAreaLight : ILight
{
	/// <summary>
	/// Returns the probability density function (pdf) value for incoming <paramref name="incident"/>
	/// direction pointing towards this <see cref="IAreaLight"/> to occur at <paramref name="point"/>.
	/// </summary>
	float ProbabilityDensity(in GeometryPoint point, in Float3 incident);
}