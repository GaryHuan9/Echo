using System;
using System.Threading;
using System.Threading.Tasks;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Directional;

/// <summary>
/// A special texture that can only be sampled based on directions.
/// </summary>
public interface IDirectionalTexture
{
	/// <summary>
	/// The average of this <see cref="IDirectionalTexture"/> across all directions.
	/// </summary>
	public RGB128 Average { get; }

	/// <summary>
	/// Invoked prior to rendering begins to perform any initialization work this <see cref="IDirectionalTexture"/> need.
	/// Other members defined in this interface will/should not be used before this method is invoked at least once.
	/// </summary>
	public void Prepare() { }

	/// <summary>
	/// Evaluates this <see cref="IDirectionalTexture"/> at <paramref name="incident"/>.
	/// </summary>
	/// <param name="incident">The unit local direction to evaluate at.</param>
	/// <returns>The <see cref="RGB128"/> value evaluated.</returns>
	/// <seealso cref="Sample"/>
	public RGB128 Evaluate(in Float3 incident);

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="incident"/> with <see cref="Sample"/>.
	/// </summary>
	/// <param name="incident">The unit local direction that was selected.</param>
	/// <returns>The probability density function (pdf) value of the selection.</returns>
	/// <seealso cref="Sample"/>
	public float ProbabilityDensity(in Float3 incident) => Sample2D.UniformSpherePdf;

	/// <summary>
	/// Samples <paramref name="incident"/> for this <see cref="IDirectionalTexture"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample2D"/> used to sample <paramref name="incident"/>.</param>
	/// <param name="incident">The unit local direction specifically sampled for this texture.</param>
	/// <returns>The <see cref="Probable{T}"/> value evaluated at <paramref name="incident"/>.</returns>
	public Probable<RGB128> Sample(Sample2D sample, out Float3 incident)
	{
		incident = sample.UniformSphere;
		return (Evaluate(incident), Sample2D.UniformSpherePdf);
	}
}

public static class IDirectionalTextureExtensions
{
	/// <summary>
	/// Explicitly calculates a converged value for <see cref="IDirectionalTexture.Average"/> using Monte Carlo sampling.
	/// </summary>
	public static RGB128 ConvergeAverage(this IDirectionalTexture texture, int sampleCount = (int)1E6)
	{
		using var sums = new ThreadLocal<SumPackage>(SumPackage.factory, true);

		int size = (int)Math.Sqrt(sampleCount);
		float sizeR = 1f / size;
		int count = size * size;

		//Sample random directions using stratified sampling
		Parallel.For(0, count, index =>
		{
			// ReSharper disable once AccessToDisposedClosure
			SumPackage package = sums.Value;

			Int2 strata = new Int2(index % size, index / size);
			Float2 position = strata + package.random.Next2();
			Sample2D sample = (Sample2D)(position * sizeR);

			package.Sum += texture.Evaluate(sample.UniformSphere);
		});

		//Total the sums for individual threads
		Summation sum = Summation.Zero;

		foreach (SumPackage package in sums.Values) sum += package.Sum;

		return (RGB128)(sum.Result / count);
	}

	class SumPackage
	{
		SumPackage()
		{
			random = new SquirrelPrng();
			Sum = Summation.Zero;
		}

		public readonly Prng random;
		public Summation Sum { get; set; }

		public static readonly Func<SumPackage> factory = () => new SumPackage();
	}
}