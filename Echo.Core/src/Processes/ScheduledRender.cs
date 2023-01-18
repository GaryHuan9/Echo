using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Processes.Preparation;
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

		var builder = ImmutableArray.CreateBuilder<Operation>();
		builder.Add(preparationOperation);
		builder.AddRange(evaluationOperations);
		if (compositionOperation != null) builder.Add(compositionOperation);
		operations = builder.ToImmutable();
	}

	/// <summary>
	/// The <see cref="RenderProfile"/> that defines this <see cref="ScheduledRender"/>.
	/// </summary>
	public readonly RenderProfile profile;

	/// <summary>
	/// The destination <see cref="RenderTexture"/> for this <see cref="ScheduledRender"/>.
	/// </summary>
	public readonly RenderTexture texture;

	/// <summary>
	/// The first <see cref="Operation"/> to be completed to prepare for the render.
	/// </summary>
	public readonly PreparationOperation preparationOperation;

	/// <summary>
	/// The main <see cref="Operation"/>s of the render, consists of a series of evaluations on the <see cref="PreparedScene"/>. 
	/// </summary>
	public readonly ImmutableArray<EvaluationOperation> evaluationOperations;

	/// <summary>
	/// The final <see cref="Operation"/> of the render which combines the evaluations together into a final image.
	/// </summary>
	/// <remarks>This is null if the <see cref="RenderProfile"/> does not have any <see cref="ICompositeLayer"/>.</remarks>
	public readonly CompositionOperation compositionOperation;

	/// <summary>
	/// A list of <see cref="Operation"/> for this <see cref="ScheduledRender"/> in their execution order.
	/// </summary>
	/// <remarks>This is simply an aggregate of <see cref="preparationOperation"/>, <see cref="evaluationOperations"/>, and <see cref="compositionOperation"/>.</remarks>
	public readonly ImmutableArray<Operation> operations;

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
	public bool IsCompleted => LastOperation.IsCompleted;

	int _currentIndex;

	/// <summary>
	/// An index in <see cref="operations"/> that is the currently executing <see cref="Operation"/>.
	/// </summary>
	/// <remarks> If all <see cref="operations"/> are completed, this returns the length of
	/// <see cref="operations"/>. If no <see cref="Operation"/> has started, this returns zero.</remarks>
	public int CurrentIndex
	{
		get
		{
			while (_currentIndex < operations.Length && operations[_currentIndex].IsCompleted) ++_currentIndex;
			return _currentIndex;
		}
	}

	/// <summary>
	/// A very rough estimate of the overall progress.
	/// </summary>
	/// <remarks>For more accuracy, use <see cref="Operation.Progress"/> on the individual operations.</remarks>
	public float Progress
	{
		get
		{
			if (IsCompleted) return 1f;
			int index = CurrentIndex;

			float progress = (float)operations[index].Progress;
			return (index + progress) / operations.Length;
		}
	}

	Operation LastOperation => operations[^1];

	/// <summary>
	/// Blocks the calling thread until <see cref="IsCompleted"/> is true or if this <see cref="ScheduledRender"/> is aborted.
	/// </summary>
	public void Await()
	{
		if (device.Disposed) return;
		device.Operations.Await(LastOperation);
	}

	/// <summary>
	/// Stops this <see cref="ScheduledRender"/> as soon as possible.
	/// </summary>
	public void Abort()
	{
		if (device.Disposed) return;
		foreach (Operation operation in operations) device.Abort(operation);
	}

	/// <summary>
	/// Creates and schedules a new render onto a <see cref="Device"/>.
	/// </summary>
	/// <param name="device">The active <see cref="Device"/> to schedule to.</param>
	/// <param name="profile">The profile used to define the render.</param>
	/// <returns>A <see cref="ScheduledRender"/> that encapsulates objects related to the render.</returns>
	public static ScheduledRender Create(Device device, RenderProfile profile) => new(device, profile);

	static PreparationOperation SchedulePreparationOperation(Device device, ScheduledRender render)
	{
		var factory = new PreparationOperation.Factory(render.profile.Scene, render.boxedScene);
		return (PreparationOperation)device.Schedule(factory);
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
		var layers = render.profile.CompositionLayers;
		if (layers.IsDefaultOrEmpty) return null;
		var factory = new CompositionOperation.Factory(render.texture, layers);
		return (CompositionOperation)device.Schedule(factory);
	}
}