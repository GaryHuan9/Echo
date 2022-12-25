using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Processes.Evaluation;

/// <summary>
/// An <see cref="Operation{T}"/> that performs one evaluation onto a <see cref="PreparedScene"/>.
/// </summary>
public sealed class EvaluationOperation : Operation<EvaluatorStatistics>
{
	EvaluationOperation(ImmutableArray<IWorker> workers,
						ImmutableArray<Int2> tilePositions, IEvaluationLayer destination,
						StrongBox<PreparedScene> boxedScene, EvaluationProfile profile) :
		base(workers, (uint)tilePositions.Length)
	{
		this.tilePositions = tilePositions;
		this.destination = destination;
		this.boxedScene = boxedScene;
		this.profile = profile;

		distributions = new ContinuousDistribution[workers.Length];
		allocators = new Allocator[workers.Length];

		for (int i = 0; i < workers.Length; i++)
		{
			distributions[i] = profile.Distribution with { };
			allocators[i] = new Allocator();
		}
	}

	public readonly ImmutableArray<Int2> tilePositions;
	public readonly IEvaluationLayer destination;
	readonly StrongBox<PreparedScene> boxedScene;
	readonly EvaluationProfile profile;

	readonly ContinuousDistribution[] distributions;
	readonly Allocator[] allocators;

	protected override void Execute(ref Procedure procedure, IWorker worker, ref EvaluatorStatistics statistics)
	{
		var distribution = distributions[worker.Index];
		var allocator = allocators[worker.Index];
		PreparedScene scene = boxedScene.Value;

		Int2 tilePosition = tilePositions[(int)procedure.index];
		IEvaluationWriteTile tile = destination.CreateTile(tilePosition);

		Ensure.IsNotNull(scene);
		Ensure.IsNotNull(tile);

		procedure.Begin((uint)tile.Size.Y);

		Int2 min = tile.Min;
		Int2 max = tile.Max;

		for (int y = min.Y; y < max.Y; y++)
		{
			for (int x = min.X; x < max.X; x++)
			{
				Int2 position = new Int2(x, y);
				Accumulator accumulator = new();

				var spawner = new RaySpawner(destination, position);

				int epoch = 0;

				do
				{
					++epoch;

					distribution.BeginSeries(position);

					for (int i = 0; i < distribution.Extend; i++)
					{
						distribution.BeginSession();

						var cameraSample = new CameraSample(distribution);
						Ray ray = scene.camera.SpawnRay(cameraSample, spawner);

						Float4 evaluated = profile.Evaluator.Evaluate
						(
							scene, ray, distribution,
							allocator, ref statistics
						);

						statistics.Report("Sample/Evaluated");

						allocator.Release();

						if (!accumulator.Add(evaluated)) statistics.Report("Sample/Rejected");
					}
				}
				while (epoch < profile.MaxEpoch && (epoch < profile.MinEpoch || accumulator.Noise.MaxComponent > profile.NoiseThreshold));

				tile[position] = accumulator.Value;
				statistics.Report("Pixel/Evaluated");
			}

			worker.CheckSchedule();
			procedure.Advance();
		}

		destination.Apply(tile);
	}

	/// <summary>
	/// An implementation of <see cref="IOperationFactory"/> for <see cref="EvaluationOperation"/>.
	/// </summary>
	public readonly struct Factory : IOperationFactory
	{
		public Factory(PreparedScene scene, RenderBuffer renderBuffer, EvaluationProfile profile) :
			this(new StrongBox<PreparedScene>(scene), renderBuffer, profile) { }

		public Factory(StrongBox<PreparedScene> boxedScene, RenderBuffer renderBuffer, EvaluationProfile profile)
		{
			profile.Validate();
			this.boxedScene = boxedScene;
			this.renderBuffer = renderBuffer;
			this.profile = profile;
		}

		readonly StrongBox<PreparedScene> boxedScene;
		readonly RenderBuffer renderBuffer;
		readonly EvaluationProfile profile;

		/// <inheritdoc/>
		public Operation CreateOperation(ImmutableArray<IWorker> workers)
		{
			IEvaluationLayer destination = profile.Evaluator.CreateOrClearLayer(renderBuffer, profile.TargetLayer);
			var tilePositions = profile.Pattern.CreateSequence(renderBuffer.size.CeiledDivide(renderBuffer.tileSize));
			return new EvaluationOperation(workers, tilePositions.ToImmutableArray(), destination, boxedScene, profile);
		}
	}
}