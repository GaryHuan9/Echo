﻿using System.Collections.Immutable;
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

public sealed partial class EvaluationOperation : Operation<EvaluationStatistics>
{
	// ReSharper disable LocalVariableHidesMember
	EvaluationOperation(EvaluationProfile profile, ImmutableArray<IWorker> workers, ImmutableArray<Context> contexts)
		: base(workers, Construct(profile, out var tilePositions, out var destination))
	// ReSharper restore LocalVariableHidesMember
	{
		this.profile = profile;
		this.tilePositions = tilePositions;
		this.destination = destination;
		this.contexts = contexts;
	}

	public readonly EvaluationProfile profile;
	public readonly ImmutableArray<Int2> tilePositions;
	public readonly IEvaluationLayer destination;

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

	static uint Construct(EvaluationProfile profile, out ImmutableArray<Int2> tilePositions, out IEvaluationLayer destination)
	{
		Int2 tileRange = profile.Buffer.size.CeiledDivide(profile.Buffer.tileSize);
		tilePositions = profile.Pattern.CreateSequence(tileRange).ToImmutableArray();
		destination = profile.Evaluator.CreateOrClearLayer(profile.Buffer);

		return (uint)tilePositions.Length;
	}

	readonly record struct Context(ContinuousDistribution Distribution, Allocator Allocator);
}