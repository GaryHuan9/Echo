using System;
using System.Collections.Immutable;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Operations;

/// <summary>
/// An implementation of <see cref="IOperationFactory"/> for tiled evaluation.
/// </summary>
public class TiledEvaluationFactory : IOperationFactory
{
	Context[] contexts; //cache contexts to reuse them

	NotNull<TiledEvaluationProfile> _nextProfile;

	/// <summary>
	/// The next <see cref="TiledEvaluationProfile"/> to use.
	/// </summary>
	/// <remarks>Cannot be null.</remarks>
	public TiledEvaluationProfile NextProfile
	{
		get => _nextProfile;
		set => _nextProfile = value;
	}

	/// <inheritdoc />
	public Common.Compute.Operation CreateOperation(ImmutableArray<IWorker> workers)
	{
		//Validate profile
		var profile = NextProfile ?? throw ExceptionHelper.Invalid(nameof(NextProfile), InvalidType.isNull);
		profile.Validate();

		//Create render buffer writer
		if (profile.Buffer.TryGetWriter(profile.Evaluator.Destination, out RenderBuffer.Writer writer)) { }
		else throw new Exception($"Invalid destination layer label assigned to {profile.Evaluator}.");

		//Create tile sequence and contexts
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		Int2[] tileSequence = profile.Pattern.CreateSequence(size);

		CreateContexts(profile, workers.Length);

		return new Operation
		(
			workers, profile, writer,
			tileSequence.ToImmutableArray(),
			contexts.ToImmutableArray()
		);
	}

	void CreateContexts(TiledEvaluationProfile profile, int population)
	{
		if (contexts == null || contexts.Length < population) Array.Resize(ref contexts, population);

		ContinuousDistribution source = profile.Distribution;

		foreach (ref Context context in contexts.AsSpan(0, population))
		{
			if (context.Distribution != source) context = context with { Distribution = source with { } };
			if (context.Allocator == null) context = context with { Allocator = new Allocator() };
		}
	}

	readonly record struct Context(ContinuousDistribution Distribution, Allocator Allocator);

	class Operation : Operation<EvaluationStatistics>
	{
		public Operation(ImmutableArray<IWorker> workers,
						 TiledEvaluationProfile profile, RenderBuffer.Writer writer,
						 ImmutableArray<Int2> tileSequence, ImmutableArray<Context> contexts) : base(workers, (uint)tileSequence.Length)
		{
			this.profile = profile;
			this.writer = writer;
			this.tileSequence = tileSequence;
			this.contexts = contexts;
		}

		readonly TiledEvaluationProfile profile;
		readonly RenderBuffer.Writer writer;

		readonly ImmutableArray<Int2> tileSequence;
		readonly ImmutableArray<Context> contexts;

		protected override void Execute(ref Procedure procedure, IWorker worker, ref EvaluationStatistics statistics)
		{
			(ContinuousDistribution distribution, Allocator allocator) = contexts[worker.Index];

			Evaluator evaluator = profile.Evaluator;
			PreparedScene scene = profile.Scene;
			RenderBuffer buffer = profile.Buffer;

			Int2 min = tileSequence[(int)procedure.index] * profile.TileSize;
			Int2 max = Int2.Min(buffer.size, min + (Int2)profile.TileSize);

			procedure.Begin((uint)(max - min).Product);

			for (int y = min.Y; y < max.Y; y++)
			for (int x = min.X; x < max.X; x++)
			{
				worker.CheckSchedule();

				Int2 position = new Int2(x, y);
				Accumulator accumulator = new();

				var spawner = new RaySpawner(buffer, position);

				int epoch = 0;

				do
				{
					++epoch;

					distribution.BeginSeries(position);

					for (int i = 0; i < distribution.Extend; i++)
					{
						distribution.BeginSession();

						Ray ray = scene.camera.SpawnRay(new CameraSample(distribution), spawner);
						Float4 evaluated = evaluator.Evaluate(scene, ray, distribution, allocator);

						statistics.Report("Evaluated Sample");

						allocator.Release();

						if (!accumulator.Add(evaluated)) statistics.Report("Rejected Sample");
					}
				}
				while (epoch < profile.MaxEpoch && (epoch < profile.MinEpoch || accumulator.Noise.MaxComponent > profile.NoiseThreshold));

				writer(position, accumulator);
				statistics.Report("Pixel");
				procedure.Advance();
			}
		}
	}
}