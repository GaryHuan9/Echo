using System.Collections.Immutable;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Operations;

public sealed class TiledEvaluationOperation : Operation<EvaluationStatistics>
{
	internal TiledEvaluationOperation(ImmutableArray<IWorker> workers, TiledEvaluationProfile profile,
									  ImmutableArray<Int2> tilePositions, ImmutableArray<Context> contexts)
		: base(workers, (uint)tilePositions.Length)
	{
		this.profile = profile;
		this.tilePositions = tilePositions;
		this.contexts = contexts;

		destination = profile.Evaluator.CreateOrClearLayer(profile.Buffer);
	}

	public readonly TiledEvaluationProfile profile;
	public readonly ImmutableArray<Int2> tilePositions;
	public readonly ITiledEvaluationLayer destination;

	readonly ImmutableArray<Context> contexts;

	protected override void Execute(ref Procedure procedure, IWorker worker, ref EvaluationStatistics statistics)
	{
		(ContinuousDistribution distribution, Allocator allocator) = contexts[worker.Index];

		Evaluator evaluator = profile.Evaluator;
		PreparedScene scene = profile.Scene;
		RenderBuffer buffer = profile.Buffer;

		Int2 tilePosition = tilePositions[(int)procedure.index];
		IEvaluationWriteTile tile = destination.CreateTile(tilePosition);

		procedure.Begin((uint)tile.Size.Y);

		Int2 min = tile.Min;
		Int2 max = tile.Max;

		for (int y = min.Y; y < max.Y; y++)
		{
			for (int x = min.X; x < max.X; x++)
			{
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

				tile[position] = accumulator.Value;
				statistics.Report("Pixel");
			}

			worker.CheckSchedule();
			procedure.Advance();
		}

		destination.Apply(tile);
	}

	internal readonly record struct Context(ContinuousDistribution Distribution, Allocator Allocator);
}