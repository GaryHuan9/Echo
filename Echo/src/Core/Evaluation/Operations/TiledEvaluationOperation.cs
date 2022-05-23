using System;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
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

	public override void Prepare(int population)
	{
		base.Prepare(population);

		//Validate profile
		profile = Profile ?? throw ExceptionHelper.Invalid(nameof(Profile), InvalidType.isNull);
		profile.Validate();

		//Create render buffer writer
		if (profile.Buffer.TryGetWriter(profile.Evaluator.Destination, out writer)) { }
		else throw new Exception("Invalid destination layer assigned to evaluator.");

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

		Statistics statistics = default;
		statistics.Report("hehe");
		statistics.Report("haha");
		statistics.Report("huhu");

		statistics.Report("this is a very long test");
		statistics.Report("duplicate");
		statistics.Report("duplicate");

		statistics.Report("ignored because of invalid syntax --");
		statistics.Report("not ignored because vali");
		statistics.Report("not ignored again h nice");

		statistics.Report("Does it still work");

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

			writer(position, accumulator);
		}

		return true;
	}

	record struct Context(ContinuousDistribution Distribution, Allocator Allocator);
}