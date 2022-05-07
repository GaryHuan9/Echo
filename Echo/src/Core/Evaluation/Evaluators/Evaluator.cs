using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	protected Evaluator() { }

	protected Evaluator(Evaluator source)
	{
		allocator = source.allocator with { };
		Distribution = source.Distribution with { };
	}

	protected readonly Allocator allocator = new();

	readonly NotNull<ContinuousDistribution> _distribution = new StratifiedDistribution();

	public ContinuousDistribution Distribution
	{
		get => _distribution;
		init => _distribution = value;
	}

	/// <summary>
	/// Evaluates <see cref="Scene"/> based on <see cref="Distribution"/> using many samples.
	/// </summary>
	/// <param name="scene"></param>
	/// <param name="region"></param>
	/// <param name="accumulator">Samples of the evaluation are added to this <see cref="Accumulator"/>.</param>
	/// <returns>The number of invalid samples that are rejected.</returns>
	/// <remarks>The exact number of samples that are used to evaluates <see cref="Scene"/> is <see cref="ContinuousDistribution.Extend"/>.</remarks>
	public abstract uint Evaluate(PreparedScene scene, TextureRegion region, ref Accumulator accumulator);
}

public abstract record Evaluator<T> : Evaluator where T : IColor<T>
{
	public sealed override uint Evaluate(PreparedScene scene, TextureRegion region, ref Accumulator accumulator)
	{
		uint rejection = 0;

		for (int i = 0; i < Distribution.Extend; i++)
		{
			Distribution.BeginSession();

			Ray ray = scene.camera.SpawnRay(new CameraSample(Distribution), region);

			Float4 sampled = Evaluate(ray).ToFloat4();
			if (!accumulator.Add(sampled)) ++rejection;

			allocator.Release();
		}

		return rejection;
	}

	/// <summary>
	/// Evaluates a <see cref="PreparedScene"/> through a <see cref="Ray"/>.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> to evaluate.</param>
	/// <param name="ray">The <see cref="Ray"/> that we should start evaluation on.</param>
	/// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
	/// <remarks>The implementation do not need to invoke <see cref="Allocator.Release"/> before this method returns.</remarks>
	protected abstract T Evaluate(PreparedScene scene, in Ray ray);
}