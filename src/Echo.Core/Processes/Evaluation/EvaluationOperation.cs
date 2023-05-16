using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Statistics;
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
						EvaluationProfile profile, StrongBox<PreparedScene> boxedScene) :
		base(workers, (uint)tilePositions.Length)
	{
		this.tilePositions = tilePositions;
		this.destination = destination;
		this.profile = profile;
		this.boxedScene = boxedScene;

		distributions = new ContinuousDistribution[workers.Length];
		allocators = new Allocator[workers.Length];

		for (int i = 0; i < workers.Length; i++)
		{
			distributions[i] = profile.Distribution with { };
			allocators[i] = new Allocator();
		}
	}

	static EvaluationOperation()
	{
		var statistics = new EvaluatorStatistics();
		samplesEvaluatedEventRowIndex = -1;

		for (int i = 0; i < EventRowCount; i++)
		{
			string label = statistics[i].Label;
			if (label != SamplesEvaluatedEventRowLabel) continue;
			samplesEvaluatedEventRowIndex = i;
			break;
		}

		Ensure.IsFalse(samplesEvaluatedEventRowIndex < 0);
	}

	public readonly ImmutableArray<Int2> tilePositions;
	public readonly IEvaluationLayer destination;
	public readonly EvaluationProfile profile;

	readonly StrongBox<PreparedScene> boxedScene;
	readonly ContinuousDistribution[] distributions;
	readonly Allocator[] allocators;

	const string SamplesEvaluatedEventRowLabel = "Sample/Evaluated";
	static readonly int samplesEvaluatedEventRowIndex;

	/// <summary>
	/// The total number of samples used for this <see cref="EvaluationOperation"/>.
	/// </summary>
	public ulong TotalSamples
	{
		get
		{
			EventRow row = GetEventRow(samplesEvaluatedEventRowIndex);
			Ensure.AreEqual(row.Label, SamplesEvaluatedEventRowLabel);
			return row.Count;
		}
	}

	protected override void Execute(ref Procedure procedure, IWorker worker, ref EvaluatorStatistics statistics)
	{
		var distribution = distributions[worker.Index];
		var allocator = allocators[worker.Index];
		PreparedScene scene = boxedScene.Value;

		if (scene == null) return; //Cannot do anything without a scene lol (happens if the preparation operation is aborted)

		Int2 tilePosition = tilePositions[(int)procedure.index];
		IEvaluationWriteTile tile = destination.CreateTile(tilePosition);
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
		public Factory(PreparedScene scene, RenderTexture renderTexture, EvaluationProfile profile) :
			this(new StrongBox<PreparedScene>(scene), renderTexture, profile) { }

		public Factory(StrongBox<PreparedScene> boxedScene, RenderTexture renderTexture, EvaluationProfile profile)
		{
			profile.Validate();
			this.boxedScene = boxedScene;
			this.renderTexture = renderTexture;
			this.profile = profile;
		}

		readonly StrongBox<PreparedScene> boxedScene;
		readonly RenderTexture renderTexture;
		readonly EvaluationProfile profile;

		/// <inheritdoc/>
		public Operation CreateOperation(ImmutableArray<IWorker> workers)
		{
			IEvaluationLayer destination = profile.Evaluator.CreateOrClearLayer(renderTexture, profile.TargetLayer);
			var tilePositions = profile.Pattern.CreateSequence(renderTexture.size.CeiledDivide(renderTexture.tileSize));
			return new EvaluationOperation(workers, tilePositions.ToImmutableArray(), destination, profile, boxedScene);
		}
	}
}