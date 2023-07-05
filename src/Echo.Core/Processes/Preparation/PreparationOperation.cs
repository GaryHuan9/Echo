using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Processes.Preparation;

public sealed class PreparationOperation : AsyncOperation
{
	PreparationOperation(ImmutableArray<IWorker> workers, RenderProfile renderProfile, StrongBox<PreparedScene> boxedScene) : base(workers)
	{
		this.renderProfile = renderProfile;
		this.boxedScene = boxedScene;
	}

	readonly RenderProfile renderProfile;
	readonly StrongBox<PreparedScene> boxedScene;

	/// <summary>
	/// The <see cref="PreparedScene"/> once preparation is completed, or null otherwise.
	/// </summary>
	public PreparedScene PreparedScene => boxedScene.Value;

	protected override ComputeTask Execute()
	{
		ScenePreparer preparer = new ScenePreparer(renderProfile.Scene);
		PreparedScene prepared = preparer.Prepare(renderProfile.CameraName);
		Volatile.Write(ref boxedScene.Value, prepared);
		return ComputeTask.CompletedTask;
	}

	public readonly struct Factory : IOperationFactory
	{
		public Factory(RenderProfile renderProfile, StrongBox<PreparedScene> boxedScene)
		{
			this.renderProfile = renderProfile;
			this.boxedScene = boxedScene;
		}

		readonly RenderProfile renderProfile;
		readonly StrongBox<PreparedScene> boxedScene;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new PreparationOperation(workers, renderProfile, boxedScene);
	}
}