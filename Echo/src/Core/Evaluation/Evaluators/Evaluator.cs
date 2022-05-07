using System;
using CodeHelpers;
using CodeHelpers.Diagnostics;
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
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	protected Evaluator() { }

	protected Evaluator(Evaluator source)
	{
		allocator = source.allocator with { };
		Distribution = source.Distribution with { };
		DestinationLabel = source.DestinationLabel;
	}

	protected readonly Allocator allocator = new();

	readonly NotNull<ContinuousDistribution> _distribution = new UniformDistribution();
	readonly NotNull<string> _destinationLabel = "main";

	public ContinuousDistribution Distribution
	{
		get => _distribution;
		init => _distribution = value;
	}

	public string DestinationLabel
	{
		get => _destinationLabel;
		init => _destinationLabel = value;
	}

	/// <summary>
	/// Evaluates a <see cref="PreparedScene"/> based on <see cref="Distribution"/> using many samples.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> to evaluate.</param>
	/// <param name="spawner">The <see cref="RaySpawner"/> used to generate <see cref="Ray"/>s.</param>
	/// <param name="accumulator">Samples of the evaluation are added to this <see cref="Accumulator"/>.</param>
	/// <returns>The number of invalid samples that are rejected.</returns>
	/// <remarks>The exact number of samples that are used to evaluates <see cref="Scene"/> is <see cref="ContinuousDistribution.Extend"/>.</remarks>
	public abstract uint Evaluate(PreparedScene scene, in RaySpawner spawner, ref Accumulator accumulator);

	/// <summary>
	/// Stores the result of an <see cref="Accumulator"/> to a <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="buffer">The <see cref="RenderBuffer"/> to store the result in.</param>
	/// <param name="position">The destination coordinate in the <see cref="RenderBuffer"/>.</param>
	/// <param name="accumulator">The <see cref="Accumulator"/> from which the result is extracted.</param>
	public abstract void Store(RenderBuffer buffer, Int2 position, in Accumulator accumulator);
}

public abstract record Evaluator<T> : Evaluator where T : IColor<T>
{
	protected Evaluator() { }

	protected Evaluator(Evaluator<T> source) : base(source)
	{
		renderBuffer = source.renderBuffer;
		destination = source.destination;
	}

	RenderBuffer renderBuffer;
	TextureGrid<T> destination;

	public sealed override uint Evaluate(PreparedScene scene, in RaySpawner spawner, ref Accumulator accumulator)
	{
		Distribution.BeginSeries(spawner.position);

		uint rejectionCount = 0;

		for (int i = 0; i < Distribution.Extend; i++)
		{
			Distribution.BeginSession();

			Ray ray = scene.camera.SpawnRay(new CameraSample(Distribution), spawner);
			if (!accumulator.Add(Evaluate(scene, ray).ToFloat4())) ++rejectionCount;

			allocator.Release();
		}

		return rejectionCount;
	}

	public override void Store(RenderBuffer buffer, Int2 position, in Accumulator accumulator)
	{
		if (renderBuffer != buffer) FindDestination(buffer);
		destination[position] = default(T)!.FromFloat4(accumulator.Value);
	}

	/// <summary>
	/// Evaluates a <see cref="PreparedScene"/> through a <see cref="Ray"/>.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> to evaluate.</param>
	/// <param name="ray">The <see cref="Ray"/> that we should start evaluation on.</param>
	/// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
	/// <remarks>The implementation do not need to invoke <see cref="Allocator.Release"/> before this method returns.</remarks>
	protected abstract T Evaluate(PreparedScene scene, in Ray ray);

	void FindDestination(RenderBuffer buffer)
	{
		bool found = buffer.TryGetLayer(DestinationLabel, out destination);

		Assert.IsTrue(found);
		renderBuffer = buffer;
	}
}