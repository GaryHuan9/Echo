using System;
using System.Collections.Immutable;
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

public sealed class TiledEvaluationOperation : Operation<EvaluationStatistics>
{
	internal TiledEvaluationOperation(ImmutableArray<IWorker> workers, TiledEvaluationProfile profile,
									  ImmutableArray<Int2> tileSequence, ImmutableArray<Context> contexts)
		: base(workers, (uint)tileSequence.Length)
	{
		this.profile = profile;
		this.tileSequence = tileSequence;
		this.contexts = contexts;

		//Create render buffer writer
		if (profile.Buffer.TryGetWriter(profile.Evaluator.Destination, out writer)) { }
		else throw new Exception($"Invalid destination assigned to {profile.Evaluator}.");
	}

	public readonly TiledEvaluationProfile profile;
	public readonly ImmutableArray<Int2> tileSequence;

	readonly ImmutableArray<Context> contexts;
	readonly RenderBuffer.Writer writer;

	public void GetTileMinMax(uint procedureIndex, out Int2 min, out Int2 max)
	{
		min = tileSequence[(int)procedureIndex] * profile.TileSize;
		max = profile.Buffer.size.Min(min + (Int2)profile.TileSize);
	}

	protected override void Execute(ref Procedure procedure, IWorker worker, ref EvaluationStatistics statistics)
	{
		(ContinuousDistribution distribution, Allocator allocator) = contexts[worker.Index];

		Evaluator evaluator = profile.Evaluator;
		PreparedScene scene = profile.Scene;
		RenderBuffer buffer = profile.Buffer;

		GetTileMinMax(procedure.index, out Int2 min, out Int2 max);

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

	internal readonly record struct Context(ContinuousDistribution Distribution, Allocator Allocator);
}