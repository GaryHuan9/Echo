using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Pixels;

namespace EchoRenderer.Textures.Directional
{
	/// <summary>
	/// A special texture that can only be sampled based on directions.
	/// </summary>
	public interface IDirectionalTexture
	{
		/// <summary>
		/// Invoked prior to rendering begins to perform any initialization work this <see cref="IDirectionalTexture"/> need.
		/// Other methods defined in this interface will/should not be invoked before this method is invoked at least once.
		/// </summary>
		virtual void Prepare() { }

		/// <summary>
		/// Evaluates this <see cref="IDirectionalTexture"/> at <paramref name="direction"/>.
		/// NOTE: <paramref name="direction"/> should be normalized and is not zero.
		/// </summary>
		Vector128<float> Evaluate(in Float3 direction);

		/// <summary>
		/// Samples this <see cref="IDirectionalTexture"/> based on <paramref name="distro"/> and outputs the
		/// <see cref="Evaluate"/> <paramref name="incidentWorld"/> direction and its <paramref name="pdf"/>.
		/// </summary>
		Vector128<float> Sample(Distro2 distro, out Float3 incidentWorld, out float pdf)
		{
			incidentWorld = distro.UniformSphere;
			pdf = Distro2.UniformSpherePDF;
			return Evaluate(incidentWorld);
		}

		/// <summary>
		/// Returns the probability density function for <paramref name="incidentWorld"/> direction on this <see cref="IDirectionalTexture"/>.
		/// </summary>
		float ProbabilityDensity(in Float3 incidentWorld) => Distro2.UniformSpherePDF;
	}
}