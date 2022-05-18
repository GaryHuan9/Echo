using System;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Compute;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Operations;

public class TiledEvaluationOperation : Operation
{
	public TiledEvaluationProfile Profile { get; set; }

	TiledEvaluationProfile profile;
	Int2[] tilePositionSequence;

	Context[] contexts;

	public override void Prepare(int population)
	{
		base.Prepare(population);

		//Validate profile
		profile = Profile ?? throw ExceptionHelper.Invalid(nameof(Profile), InvalidType.isNull);
		profile.Validate();

		//Create tile sequence
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		tilePositionSequence = profile.Pattern.CreateSequence(size);

		//Duplicate context tuples
		if ((contexts?.Length ?? 0) < population) Array.Resize(ref contexts, population);
		ContinuousDistribution source = profile.Distribution;

		foreach (ref Context context in contexts.AsSpan())
		{
			if (context.Distribution != source) context.Distribution = source with { };
			context.Allocator ??= new Allocator();
		}
	}

	protected override bool Execute(ulong procedure, IScheduler scheduler)
	{
		if (procedure >= (ulong)tilePositionSequence.Length) return false;

		(ContinuousDistribution distribution, Allocator allocator) = contexts[scheduler.Id];

		Evaluator evaluator = profile.Evaluator;
		PreparedScene scene = profile.Scene;
		RenderBuffer buffer = profile.Buffer;

		Int2 min = tilePositionSequence[procedure] * profile.TileSize;
		Int2 max = buffer.size.Min(min + (Int2)profile.TileSize);

		for (int y = min.Y; y < max.Y; y++)
		for (int x = min.X; x < max.X; x++)
		{
			scheduler.CheckSchedule();

			Int2 position = new Int2(x, y);
			Accumulator accumulator = new();

			var spawner = new RaySpawner(buffer, position);

			int epoch = 0;

			do
			{
				++epoch;

				uint rejection = 0;

				distribution.BeginSeries(position);

				for (int i = 0; i < distribution.Extend; i++)
				{
					distribution.BeginSession();

					Ray ray = scene.camera.SpawnRay(new CameraSample(distribution), spawner);
					Float4 evaluated = evaluator.Evaluate(scene, ray, distribution, allocator);

					allocator.Release();

					if (!accumulator.Add(evaluated)) ++rejection;
				}
			}
			while (epoch < profile.MaxEpoch && (epoch < profile.MinEpoch || accumulator.Noise.MaxComponent > profile.NoiseThreshold));

			// buffer.Store(position, accumulator.Value);
		}

		return true;
	}

	record struct Context(ContinuousDistribution Distribution, Allocator Allocator);
}