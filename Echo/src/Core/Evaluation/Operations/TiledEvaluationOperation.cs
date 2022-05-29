using System;
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

public class TiledEvaluationOperation : Operation<EvaluationStatistics>
{
	TiledEvaluationProfile profile;
	RenderBuffer.Writer writer;
	Int2[] tilePositionSequence;

	Context[] contexts;

	NotNull<TiledEvaluationProfile> _profile;

	public TiledEvaluationProfile Profile
	{
		get => _profile;
		set => _profile = value;
	}

	protected override ulong WarmUp(int population)
	{
		//Validate profile
		profile = Profile ?? throw ExceptionHelper.Invalid(nameof(Profile), InvalidType.isNull);
		profile.Validate();

		//Create render buffer writer
		if (profile.Buffer.TryGetWriter(profile.Evaluator.Destination, out writer)) { }
		else throw new Exception("Invalid destination layer assigned to evaluator.");

		//Create tile sequence and contexts
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		tilePositionSequence = profile.Pattern.CreateSequence(size);

		CreateContexts(population);

		return (ulong)tilePositionSequence.Length;
	}

	protected override void Execute(ulong procedure, IScheduler scheduler, ref EvaluationStatistics statistics)
	{
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

				statistics.Report("Rejected Sample", rejection);
			}
			while (epoch < profile.MaxEpoch && (epoch < profile.MinEpoch || accumulator.Noise.MaxComponent > profile.NoiseThreshold));

			statistics.Report("Pixel");
			writer(position, accumulator);
		}
	}

	void CreateContexts(int population)
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
}