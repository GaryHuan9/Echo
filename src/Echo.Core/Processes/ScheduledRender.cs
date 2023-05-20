using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.InOut;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Processes.Preparation;
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
		baseOperationIndex = device.Operations.IndexOf(operations[0]);
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
	readonly int baseOperationIndex;

	/// <summary>
	/// The <see cref="PreparedScene"/> that was created for this render.
	/// </summary>
	/// <remarks>This property returns null if the <see cref="preparationOperation"/> has not completed yet.</remarks>
	public PreparedScene Scene => boxedScene.Value;

	/// <summary>
	/// Whether the entire render is completed.
	/// </summary>
	/// <remarks>Renders with aborted <see cref="Operation"/> will also be considered as complete.</remarks>
	public bool IsCompleted => CurrentIndex == operations.Length;

	/// <summary>
	/// An index in <see cref="operations"/> that is the currently executing <see cref="Operation"/>.
	/// </summary>
	/// <remarks> If all <see cref="operations"/> are completed, this returns the length of
	/// <see cref="operations"/>. If no <see cref="Operation"/> has started, this returns zero.</remarks>
	public int CurrentIndex
	{
		get
		{
			int deviceIndex = device.Operations.CurrentIndex;
			if (deviceIndex < baseOperationIndex) return 0;
			return Math.Min(deviceIndex - baseOperationIndex, operations.Length);
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

	/// <summary>
	/// Blocks the calling thread until <see cref="IsCompleted"/> is true or if this <see cref="ScheduledRender"/> is aborted.
	/// </summary>
	public void Await()
	{
		if (device.Disposed) return;
		foreach (Operation operation in operations) device.Operations.Await(operation);
	}

	/// <summary>
	/// Similar to <see cref="Await"/>, except information about the render process is also reported through <see cref="Console.WriteLine(string)"/>.
	/// </summary>
	public void Monitor()
	{
		if (device.Disposed) return;

		try
		{
			Console.CursorVisible = false;
			PrintRenderProcess(this);
		}
		finally { Console.CursorVisible = true; }

		Await();
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
		var factory = new PreparationOperation.Factory(render.profile, render.boxedScene);
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

	static void PrintRenderProcess(ScheduledRender render)
	{
		var builder = new StringBuilder();

		foreach (Operation operation in render.operations)
		{
			while (true)
			{
				WriteOperationStatus(builder, operation);
				if (operation.IsCompleted) break;
				Thread.Sleep(16);
			}

			WriteOperationStatus(builder, operation);
			Console.WriteLine();
		}
	}

	static void WriteOperationStatus(StringBuilder builder, Operation operation)
	{
		Ensure.AreEqual(builder.Length, 0);
		TimeSpan time = operation.Time;
		builder.Append($"{operation.GetType().Name,-30} ");

		if (!operation.IsCompleted)
		{
			float progress = (float)operation.Progress;
			builder.Append($"{progress.ToInvariantPercent()} - {time.ToInvariant()}");

			if (progress > 0f)
			{
				TimeSpan timeRemain = time / progress - time;
				builder.Append($" / {timeRemain.ToInvariant()}");
			}
		}
		else
		{
			builder.Append($"Done - {operation.Time.ToInvariant()}");

			if (operation is EvaluationOperation evaluation)
			{
				ulong samples = evaluation.TotalSamples;
				float seconds = (float)time.TotalSeconds;
				ulong rate = (ulong)(samples / seconds);
				builder.Append($" @ {rate.ToInvariantMetric()}/s");
			}
		}

		Console.CursorLeft = 0;
		int padding = Console.BufferWidth - 1 - builder.Length;
		Console.Write(builder.Append(' ', padding).ToString());

		builder.Clear();
	}
}