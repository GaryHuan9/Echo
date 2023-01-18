using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures;
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

	uint _completedLayerCount;

	/// <summary>
	/// The number of completed <see cref="ICompositeLayer"/>s.
	/// </summary>
	/// <remarks>Because the <see cref="layers"/> run sequentially, all <see cref="ICompositeLayer"/>
	/// from index zero to one less than the number of this property has completed.</remarks>
	public uint CompletedLayerCount => Volatile.Read(ref _completedLayerCount);

	readonly string[] _errorMessages;

	/// <summary>
	/// Errors reported from <see cref="layers"/>.
	/// </summary>
	/// <remarks>The <see cref="string"/> at each index is the error reported from a <see cref="ICompositeLayer"/> at the same index
	/// in <see cref="layers"/>, if any. If a <see cref="ICompositeLayer"/> is completed (see <see cref="CompletedLayerCount"/>) and its
	/// corresponding message is null, then the <see cref="ICompositeLayer"/> ran successfully.</remarks>
	public ReadOnlySpan<string> ErrorMessages => _errorMessages;

	protected override async ComputeTask Execute()
	{
		var context = new Context(renderTexture, this);

		for (int i = 0; i < layers.Length; i++)
		{
			try { await layers[i].ExecuteAsync(context); }
			catch (CompositeException exception)
			{
				Volatile.Write(ref _errorMessages[i], exception.Message);
			}

			Volatile.Write(ref _completedLayerCount, (uint)i + 1);
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

		readonly List<ArrayGrid<RGB128>> temporaryTexturePool = new();

		const int TexturePoolMaxCount = 8;

		/// <inheritdoc/>
		public Int2 RenderSize => renderTexture.size;

		/// <inheritdoc/>
		public bool TryGetTexture<T, U>(string label, out U layer) where T : unmanaged, IColor<T>
																   where U : TextureGrid<T> =>
			renderTexture.TryGetLayer<T, U>(label, out layer);

		/// <inheritdoc/>
		public bool TryAddTexture(string label, TextureGrid texture) => renderTexture.TryAddLayer(label, texture);

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

		public ArrayGrid<RGB128> RetrieveTemporaryTexture()
		{
			ArrayGrid<RGB128> texture = null;

			lock (temporaryTexturePool)
			{
				if (temporaryTexturePool.Count > 0)
				{
					int end = temporaryTexturePool.Count - 1;
					texture = temporaryTexturePool[end];
					temporaryTexturePool.RemoveAt(end);
				}
			}

			return texture ?? new ArrayGrid<RGB128>(RenderSize);
		}

		public void ReleaseTemporaryTexture(ArrayGrid<RGB128> texture)
		{
			lock (temporaryTexturePool)
			{
				Ensure.IsFalse(temporaryTexturePool.Contains(texture));
				if (temporaryTexturePool.Count >= TexturePoolMaxCount) return;
				temporaryTexturePool.Add(texture);
			}
		}
	}
}