using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Scenic.Instancing;
using EchoRenderer.Scenic.Preparation;

namespace EchoRenderer.Scenic.Lights;

/// <summary>
/// An <see cref="ILight"/> <see cref="Entity"/> explicitly contained in the <see cref="Scene"/>.
/// Note that light sources created this way must be contained in the root <see cref="EntityPack"/>.
/// </summary>
public abstract class LightSource : Entity, ILight
{
	/// <summary>
	/// The main color and intensity of this <see cref="LightSource"/>.
	/// </summary>
	public Float3 Intensity { get; set; } = Float3.one;

	/// <summary>
	/// The approximated total emitted power of this <see cref="LightSource"/>.
	/// </summary>
	public virtual Float3 Power => throw new NotSupportedException();

	/// <summary>
	/// Invoked before rendering; after geometry and materials are prepared.
	/// Can be used to initialize this light to prepare it for rendering.
	/// </summary>
	public virtual void Prepare(PreparedScene scene) => Intensity = Intensity.Max(Float3.zero);

	/// <inheritdoc/>
	public abstract Float3 Sample(in GeometryPoint point, Distro2 distro, out Float3 incident, out float pdf, out float travel);
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
/// NOTE: the entirety of the light system (ie. parameters etc.) is in world space.
/// </summary>
public interface ILight
{
	/// <summary>
	/// Samples the contribution of this <see cref="ILight"/> to <paramref name="point"/>.
	/// </summary>
	Float3 Sample(in GeometryPoint point, Distro2 distro, out Float3 incident, out float pdf, out float travel);
}

/// <summary>
/// An <see cref="ILight"/> that has area, which means that is it not a singularity.
/// </summary>
public interface IAreaLight : ILight
{
	/// <summary>
	/// Returns the probability density function (pdf) for <paramref name="incident"/>
	/// to occur at <paramref name="point"/> to this <see cref="IAreaLight"/>.
	/// </summary>
	float ProbabilityDensity(in GeometryPoint point, in Float3 incident);
}