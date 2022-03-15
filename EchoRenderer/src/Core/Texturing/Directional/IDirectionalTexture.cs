using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
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
	/// Samples this <see cref="IDirectionalTexture"/> based on <paramref name="sample"/> and outputs the
	/// <see cref="Evaluate"/> <paramref name="incident"/> direction and its <paramref name="pdf"/>.
	/// </summary>
	Vector128<float> Sample(Sample2D sample, out Float3 incident, out float pdf)
	{
		incident = sample.UniformSphere;
		pdf = Sample2D.UniformSpherePdf;
		return Evaluate(incident);
	}

	/// <summary>
	/// Returns the probability density function for <paramref name="incident"/> direction on this <see cref="IDirectionalTexture"/>.
	/// </summary>
	float ProbabilityDensity(in Float3 incident) => Sample2D.UniformSpherePdf;
}
public static class IDirectionalTextureExtensions
{
	/// <summary>
	/// Explicitly calculates a converged value for <see cref="IDirectionalTexture.Average"/> using Monte Carlo sampling.
	/// </summary>
	public static Vector128<float> ConvergeAverage(this IDirectionalTexture texture, int sampleCount = 1000000)
	{
		//TODO: optimize with multithreading / Parallel.For

		IRandom random = new SquirrelRandom();
		Summation sum = Summation.Zero;

		for (int i = 0; i < sampleCount; i++)
		{
			var direction = random.NextOnSphere();
			sum += texture.Evaluate(direction);
		}

		return Sse.Divide(sum.Result, Vector128.Create((float)sampleCount));
	}
}