using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// An <see cref="Operation{T}"/> that applies a series of <see cref="ICompositeLayer"/> onto a <see cref="RenderTexture"/>.
/// </summary>
public sealed class CompositionOperation : AsyncOperation
{
	CompositionOperation(ImmutableArray<IWorker> workers, RenderTexture renderTexture, ImmutableArray<ICompositeLayer> layers) : base(workers)
	{
		this.renderTexture = renderTexture;
		this.layers = layers;
		_errorMessages = new string[layers.Length];
	}

	/// <summary>
	/// The <see cref="ICompositeContext"/> regarded by this <see cref="CompositionOperation"/>, in the order of execution.
	/// </summary>
	public readonly ImmutableArray<ICompositeLayer> layers;

	readonly RenderTexture renderTexture;

	uint _completedCount;

	/// <summary>
	/// The number of completed <see cref="ICompositeLayer"/>s.
	/// </summary>
	/// <remarks>Because the <see cref="layers"/> run sequentially, all <see cref="ICompositeLayer"/>
	/// from index zero to one less than the number of this property has completed.</remarks>
	public uint CompletedCount => Volatile.Read(ref _completedCount);

	readonly string[] _errorMessages;

	/// <summary>
	/// Errors reported from <see cref="layers"/>.
	/// </summary>
	/// <remarks>The <see cref="string"/> at each index is the error reported from a <see cref="ICompositeLayer"/> at the same index
	/// in <see cref="layers"/>, if any. If a <see cref="ICompositeLayer"/> is completed (see <see cref="CompletedCount"/>) and its
	/// corresponding message is null, then the <see cref="ICompositeLayer"/> ran successfully.</remarks>
	public ReadOnlySpan<string> ErrorMessages => _errorMessages;

	protected override async ComputeTask Execute()
	{
		var context = new Context(renderTexture, this);

		for (int i = 0; i < layers.Length; i++)
		{
			try { await layers[i].ExecuteAsync(context); }
			catch (ICompositeLayer.CompositeException exception)
			{
				Volatile.Write(ref _errorMessages[i], exception.Message);
			}

			Volatile.Write(ref _completedCount, (uint)i);
		}
	}

	/// <summary>
	/// An implementation of <see cref="IOperationFactory"/> for <see cref="CompositionOperation"/>.
	/// </summary>
	public readonly struct Factory : IOperationFactory
	{
		public Factory(RenderTexture renderTexture, ImmutableArray<ICompositeLayer> layers)
		{
			this.renderTexture = renderTexture;
			this.layers = layers;
		}

		readonly RenderTexture renderTexture;
		readonly ImmutableArray<ICompositeLayer> layers;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new CompositionOperation(workers, renderTexture, layers);
	}

	class Context : ICompositeContext
	{
		public Context(RenderTexture renderTexture, AsyncOperation operation)
		{
			this.renderTexture = renderTexture;
			this.operation = operation;
		}

		readonly RenderTexture renderTexture;
		readonly AsyncOperation operation;

		readonly List<ArrayGrid<RGB128>> temporaryBufferPool = new();

		/// <inheritdoc/>
		public Int2 RenderSize => renderTexture.size;

		/// <inheritdoc/>
		public bool TryGetTexture<T>(string label, out TextureGrid<T> texture) where T : unmanaged, IColor<T> =>
			renderTexture.TryGetTexture<T, TextureGrid<T>>(label, out texture);

		/// <inheritdoc/>
		public bool TryGetTexture<T>(string label, out SettableGrid<T> texture) where T : unmanaged, IColor<T> =>
			renderTexture.TryGetTexture<T, SettableGrid<T>>(label, out texture);

		/// <inheritdoc/>
		public ComputeTask RunAsync(ICompositeContext.Pass2D pass, Int2 size)
		{
			return operation.Schedule(EveryY, (uint)size.Y);

			void EveryY(uint y)
			{
				for (int x = 0; x < size.X; x++) pass(new Int2(x, (int)y));
			}
		}

		/// <inheritdoc/>
		public ComputeTask RunAsync(ICompositeContext.Pass1D pass, int size) => operation.Schedule(new Action<uint>(pass), (uint)size);

		/// <inheritdoc/>
		public ICompositeContext.PoolReleaseHandle FetchTemporaryTexture(out ArrayGrid<RGB128> texture)
		{
			texture = null;
			List<ArrayGrid<RGB128>> pool = temporaryBufferPool;

			lock (pool)
			{
				if (pool.Count > 0)
				{
					texture = pool[^1];
					pool.RemoveAt(pool.Count - 1);
				}
			}

			texture ??= new ArrayGrid<RGB128>(RenderSize);
			return new ICompositeContext.PoolReleaseHandle(pool, texture);
		}
	}
}