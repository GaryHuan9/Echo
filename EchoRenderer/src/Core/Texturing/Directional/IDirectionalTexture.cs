using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
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
	RGBA128 Average { get; }

	/// <summary>
	/// Invoked prior to rendering begins to perform any initialization work this <see cref="IDirectionalTexture"/> need.
	/// Other members defined in this interface will/should not be used before this method is invoked at least once.
	/// </summary>
	virtual void Prepare() { }

	/// <summary>
	/// Evaluates this <see cref="IDirectionalTexture"/> at <paramref name="direction"/>.
	/// NOTE: <paramref name="direction"/> should have a squared magnitude of exactly one.
	/// </summary>
	RGBA128 Evaluate(in Float3 direction);

	/// <summary>
	/// Samples this <see cref="IDirectionalTexture"/> based on <paramref name="sample"/> and outputs the
	/// <see cref="Evaluate"/> <paramref name="incident"/> direction and its <paramref name="pdf"/>.
	/// </summary>
	Probable<RGBA128> Sample(Sample2D sample, out Float3 incident)
	{
		incident = sample.UniformSphere;
		return (Evaluate(incident), Sample2D.UniformSpherePdf);
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
	public static RGBA128 ConvergeAverage(this IDirectionalTexture texture, int sampleCount = (int)1E6)
	{
		using ThreadLocal<SumPackage> sums = new(SumPackage.factory, true);

		//Sample random directions
		Parallel.For(0, sampleCount, _ =>
		{
			// ReSharper disable once AccessToDisposedClosure
			SumPackage package = sums.Value;

			var direction = package.random.NextOnSphere();
			package.Sum += texture.Evaluate(direction);
		});

		//Total the sums for individual threads
		Summation sum = Summation.Zero;

		foreach (SumPackage package in sums.Values) sum += package.Sum;

		return (RGBA128)(sum.Result / sampleCount);
	}

	class SumPackage
	{
		SumPackage()
		{
			random = new SquirrelRandom();
			Sum = Summation.Zero;
		}

		public readonly IRandom random;
		public Summation Sum { get; set; }

		public static readonly Func<SumPackage> factory = () => new SumPackage();
	}
}