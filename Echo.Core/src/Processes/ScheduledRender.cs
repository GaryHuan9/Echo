using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Processes;

public sealed class ScheduledRender
{
	ScheduledRender(Device device, RenderProfile profile)
	{
		profile.Validate();
		this.profile = profile;
		this.device = device;

		texture = new RenderTexture(profile.Resolution, profile.TileSize);
		preparationOperation = SchedulePreparationOperation(device, this);
		evaluationOperations = ScheduleEvaluationOperations(device, this);
		compositionOperation = ScheduleCompositionOperation(device, this);
	}

	public readonly RenderProfile profile;
	public readonly RenderTexture texture;

	public readonly AsyncOperation preparationOperation;
	public readonly CompositionOperation compositionOperation;
	public readonly ImmutableArray<EvaluationOperation> evaluationOperations;

	readonly StrongBox<PreparedScene> boxedScene = new();

	readonly Device device;

	/// <summary>
	/// The <see cref="PreparedScene"/> that was created for this render.
	/// </summary>
	/// <remarks>This property returns null if the <see cref="preparationOperation"/> has not completed yet.</remarks>
	public PreparedScene Scene => boxedScene.Value;

	/// <summary>
	/// Whether the entire render is completed.
	/// </summary>
	public bool IsCompleted => compositionOperation.IsCompleted;

	/// <summary>
	/// A very rough estimate of the overall progress.
	/// </summary>
	/// <remarks>For more accuracy, use <see cref="Operation.Progress"/> on the individual operations.</remarks>
	public float Progress
	{
		get
		{
			if (IsCompleted) return 1f;

			float result = 0f;

			result += 0.04f * (float)preparationOperation.Progress;
			result += 0.06f * (float)compositionOperation.Progress;

			float percent = 0.9f / evaluationOperations.Length;
			foreach (var operation in evaluationOperations) result += percent * (float)operation.Progress;

			return result;
		}
	}

	/// <summary>
	/// Blocks the calling thread until <see cref="IsCompleted"/> is true or if this <see cref="ScheduledRender"/> is aborted.
	/// </summary>
	public void Await()
	{
		if (device.Disposed) return;
		device.Operations.Await(compositionOperation);
	}

	/// <summary>
	/// Stops this <see cref="ScheduledRender"/> as soon as possible.
	/// </summary>
	public void Abort()
	{
		if (device.Disposed) return;
		device.Abort(preparationOperation);
		device.Abort(compositionOperation);

		foreach (var operation in evaluationOperations) device.Abort(operation);
	}

	/// <summary>
	/// Creates and schedules a new render onto a <see cref="Device"/>.
	/// </summary>
	/// <param name="device">The active <see cref="Device"/> to schedule to.</param>
	/// <param name="profile">The profile used to define the render.</param>
	/// <returns>A <see cref="ScheduledRender"/> that encapsulates objects related to the render.</returns>
	public static ScheduledRender Create(Device device, RenderProfile profile) => new(device, profile);

	static AsyncOperation SchedulePreparationOperation(Device device, ScheduledRender render)
	{
		return (AsyncOperation)device.Schedule(AsyncOperation.New(Prepare));

		ComputeTask Prepare(AsyncOperation operation)
		{
			var preparer = new ScenePreparer(render.profile.Scene);
			render.boxedScene.Value = preparer.Prepare();
			return ComputeTask.CompletedTask;
		}
	}

	static ImmutableArray<EvaluationOperation> ScheduleEvaluationOperations(Device device, ScheduledRender render)
	{
		var builder = ImmutableArray.CreateBuilder<EvaluationOperation>(render.profile.EvaluationProfiles.Length);

		foreach (EvaluationProfile profile in render.profile.EvaluationProfiles)
		{
			var factory = new EvaluationOperation.Factory(render.boxedScene, render.texture, profile);
			builder.Add((EvaluationOperation)device.Schedule(factory));
		}

		return builder.MoveToImmutable();
	}

	static CompositionOperation ScheduleCompositionOperation(Device device, ScheduledRender render)
	{
		var factory = new CompositionOperation.Factory(render.texture, render.profile.CompositionLayers);
		return (CompositionOperation)device.Schedule(factory);
	}
}