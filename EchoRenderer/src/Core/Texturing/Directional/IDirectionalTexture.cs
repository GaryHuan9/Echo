using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Texturing.Directional;

/// <summary>
/// A special texture that can only be sampled based on directions.
/// </summary>
public interface IDirectionalTexture
{
	/// <summary>
	/// Returns the average of this <see cref="IDirectionalTexture"/> on all directions.
	/// </summary>
	Vector128<float> Average { get; }

	/// <summary>
	/// Invoked prior to rendering begins to perform any initialization work this <see cref="IDirectionalTexture"/> need.
	/// Other members defined in this interface will/should not be used before this method is invoked at least once.
	/// </summary>
	virtual void Prepare() { }

	/// <summary>
	/// Evaluates this <see cref="IDirectionalTexture"/> at <paramref name="direction"/>.
	/// NOTE: <paramref name="direction"/> should have a squared magnitude of exactly one.
	/// </summary>
	Vector128<float> Evaluate(in Float3 direction);

	/// <summary>
	/// Samples this <see cref="IDirectionalTexture"/> based on <paramref name="distro"/> and outputs the
	/// <see cref="Evaluate"/> <paramref name="incident"/> direction and its <paramref name="pdf"/>.
	/// </summary>
	Vector128<float> Sample(Distro2 distro, out Float3 incident, out float pdf)
	{
		incident = distro.UniformSphere;
		pdf = Distro2.UniformSpherePDF;
		return Evaluate(incident);
	}

	/// <summary>
	/// Returns the probability density function for <paramref name="incident"/> direction on this <see cref="IDirectionalTexture"/>.
	/// </summary>
	float ProbabilityDensity(in Float3 incident) => Distro2.UniformSpherePDF;
}