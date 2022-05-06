using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Mathematics.Randomization;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	protected Evaluator() { }

	protected Evaluator(Evaluator source)
	{
		allocator = source.allocator with { };
		Scene = source.Scene;
		Distribution = source.Distribution with { };
	}

	protected readonly Allocator allocator = new();

	readonly NotNull<PreparedScene> _scene = PreparedScene.empty;
	readonly NotNull<ContinuousDistribution> _distribution = new StratifiedDistribution();

	public PreparedScene Scene
	{
		get => _scene;
		init => _scene = value;
	}

	public ContinuousDistribution Distribution
	{
		get => _distribution;
		init => _distribution = value;
	}

	/// <summary>
	/// Evaluates <see cref="Scene"/> based on <see cref="Distribution"/> using many samples.
	/// </summary>
	/// <param name="spawner">The <see cref="RaySpawner"/> we use to create new rays for evaluation.</param>
	/// <param name="accumulator">Samples of the evaluation are added to this <see cref="Accumulator"/>.</param>
	/// <returns>The number of invalid samples that are rejected.</returns>
	/// <remarks>The exact number of samples that are used to evaluates <see cref="Scene"/> is <see cref="ContinuousDistribution.Extend"/>.</remarks>
	public abstract uint Evaluate(RaySpawner spawner, ref Accumulator accumulator);
}

public abstract record Evaluator<T> : Evaluator where T : IColor<T>
{
	public sealed override uint Evaluate(RaySpawner spawner, ref Accumulator accumulator)
	{
		uint rejection = 0;

		for (int i = 0; i < Distribution.Extend; i++)
		{
			Distribution.BeginSession();

			Ray ray = spawner.Spawn(Scene.camera, Distribution);
			Float4 sampled = Evaluate(ray).ToFloat4();
			if (!accumulator.Add(sampled)) ++rejection;

			allocator.Release();
		}

		return rejection;
	}

	/// <summary>
	/// Evaluates <see cref="Evaluator.Scene"/> through <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The <see cref="Ray"/> that we should start evaluation on.</param>
	/// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
	/// <remarks>The implementation do not need to invoke <see cref="Allocator.Release"/> before this method returns.</remarks>
	protected abstract T Evaluate(in Ray ray);
}