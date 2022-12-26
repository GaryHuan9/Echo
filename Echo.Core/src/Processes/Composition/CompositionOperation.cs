using System.Collections.Immutable;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Processes.Composition;

using CompositionLayers = ImmutableArray<ICompositeLayer>;

/// <summary>
/// An <see cref="Operation{T}"/> that applies a series of <see cref="ICompositeLayer"/> onto a <see cref="RenderBuffer"/>.
/// </summary>
public sealed class CompositionOperation : AsyncOperation
{
	CompositionOperation(ImmutableArray<IWorker> workers, RenderBuffer renderBuffer, CompositionLayers layers) : base(workers)
	{
		context = new CompositeContext(renderBuffer, this);
		this.layers = layers;
	}

	readonly CompositeContext context;
	readonly CompositionLayers layers;

	protected override async ComputeTask Execute()
	{
		foreach (ICompositeLayer layer in layers) await layer.ExecuteAsync(context);
	}

	/// <summary>
	/// An implementation of <see cref="IOperationFactory"/> for <see cref="CompositionOperation"/>.
	/// </summary>
	public readonly struct Factory : IOperationFactory
	{
		public Factory(RenderBuffer renderBuffer, CompositionLayers layers)
		{
			this.renderBuffer = renderBuffer;
			this.layers = layers;
		}

		readonly RenderBuffer renderBuffer;
		readonly CompositionLayers layers;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new CompositionOperation(workers, renderBuffer, layers);
	}
}