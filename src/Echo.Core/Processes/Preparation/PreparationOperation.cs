using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Processes.Preparation;

public sealed class PreparationOperation : AsyncOperation
{
	PreparationOperation(ImmutableArray<IWorker> workers, Scene sourceScene, StrongBox<PreparedScene> boxedScene) : base(workers)
	{
		this.sourceScene = sourceScene;
		this.boxedScene = boxedScene;
	}

	readonly Scene sourceScene;
	readonly StrongBox<PreparedScene> boxedScene;

	/// <summary>
	/// The <see cref="PreparedScene"/> once preparation is completed, or null otherwise.
	/// </summary>
	public PreparedScene PreparedScene => boxedScene.Value;

	protected override ComputeTask Execute()
	{
		var preparer = new ScenePreparer(sourceScene);
		PreparedScene prepared = preparer.Prepare();
		Volatile.Write(ref boxedScene.Value, prepared);
		return ComputeTask.CompletedTask;
	}

	public readonly struct Factory : IOperationFactory
	{
		public Factory(Scene sourceScene, StrongBox<PreparedScene> boxedScene)
		{
			this.sourceScene = sourceScene;
			this.boxedScene = boxedScene;
		}

		readonly Scene sourceScene;
		readonly StrongBox<PreparedScene> boxedScene;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new PreparationOperation(workers, sourceScene, boxedScene);
	}
}