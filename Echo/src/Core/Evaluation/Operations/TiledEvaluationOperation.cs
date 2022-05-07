using System;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Core.Compute;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Evaluation.Operations;

public class TiledEvaluationOperation : Operation
{
	public TiledEvaluationProfile Profile { get; set; }

	TiledEvaluationProfile profile;
	Int2[] tilePositionSequence;

	Evaluator[] evaluators;

	public override void Prepare(int population)
	{
		base.Prepare(population);

		//Validate profile
		profile = Profile ?? throw ExceptionHelper.Invalid(nameof(Profile), InvalidType.isNull);
		profile.Validate();

		//Create tile sequence
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		tilePositionSequence = profile.Pattern.CreateSequence(size);

		//Duplicate evaluators
		if (evaluators == null || evaluators?.Length < population) evaluators = new Evaluator[population];
		for (int i = 0; i < population; i++) evaluators[i] = profile.Evaluator with { };
	}

	protected override bool Execute(ulong procedure, Scheduler scheduler)
	{
		if (procedure >= (ulong)tilePositionSequence.Length) return false;

		Evaluator evaluator = evaluators[scheduler.id];
		PreparedScene scene = profile.Scene;

		Int2 min = tilePositionSequence[procedure] * profile.TileSize;
		Int2 max = profile.Buffer.size.Min(min + (Int2)profile.TileSize);

		for (int y = min.Y; y < max.Y; y++)
		for (int x = min.X; x < max.X; x++)
		{
			scheduler.CheckSchedule();

			Int2 position = new Int2(x, y);
			Accumulator accumulator = new();

			evaluator.Evaluate()

			int epoch = 0;

			do
			{
				++epoch;

				evaluator.Distribution.BeginSeries(position);
				evaluator.Evaluate(profile.Scene, , ref accumulator);
			}
			while (epoch < profile.MaxEpoch && (epoch < profile.MinEpoch || accumulator.Noise.MaxComponent > profile.NoiseThreshold));
		}

		return true;
	}
}