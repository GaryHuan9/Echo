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

		renderBuffer = new RenderBuffer(profile.Resolution, profile.TileSize);

		preparationOperation = SchedulePreparationOperation(device, this);
		evaluationOperations = ScheduleEvaluationOperations(device, this);
		compositionOperation = ScheduleCompositionOperation(device, this);
	}

	public readonly RenderProfile profile;
	public readonly RenderBuffer renderBuffer;

	public readonly Operation preparationOperation;
	public readonly Operation compositionOperation;
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

	public void Abort()
	{
		if (device.Disposed) return;
		device.Abort(preparationOperation);
		device.Abort(compositionOperation);

		foreach (var operation in evaluationOperations) device.Abort(operation);
	}

	public void Await()
	{
		if (device.Disposed) return;
		device.Operations.Await(compositionOperation);
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
			var factory = new EvaluationOperation.Factory(render.boxedScene, render.renderBuffer, profile);
			builder.Add((EvaluationOperation)device.Schedule(factory));
		}

		return builder.MoveToImmutable();
	}

	static AsyncOperation ScheduleCompositionOperation(Device device, ScheduledRender render)
	{
		var factory = new CompositionOperation.Factory(render.renderBuffer, render.profile.CompositionLayers);
		return (AsyncOperation)device.Schedule(factory);
	}
}