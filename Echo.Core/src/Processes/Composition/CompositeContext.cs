using System;
using System.Collections.Generic;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public class CompositeContext
{
	public CompositeContext(RenderBuffer renderBuffer, AsyncOperation operation)
	{
		this.renderBuffer = renderBuffer;
		this.operation = operation;
	}

	readonly RenderBuffer renderBuffer;
	readonly AsyncOperation operation;

	readonly List<ArrayGrid<RGB128>> temporaryBufferPool = new();
	const int BufferPoolMaxCount = 8;

	public Int2 RenderSize => renderBuffer.size;
	
	/// <inheritdoc cref="RenderBuffer.TryGetTexture{T, U}"/>
	public bool TryGetBuffer<T>(string label, out TextureGrid<T> buffer) where T : unmanaged, IColor<T> =>
		renderBuffer.TryGetTexture<T, TextureGrid<T>>(label, out buffer);
	
	/// <inheritdoc cref="RenderBuffer.TryGetTexture{T, U}"/>
	public bool TryGetBuffer<T>(string label, out SettableGrid<T> buffer) where T : unmanaged, IColor<T> =>
		renderBuffer.TryGetTexture<T, SettableGrid<T>>(label, out buffer);

	/// <summary>
	/// Runs a <see cref="Pass2D"/> on every position within <see cref="RenderSize"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass2D pass) => RunAsync(pass, RenderSize);

	/// <summary>
	/// Runs a <see cref="Pass2D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass2D pass, Int2 size)
	{
		return operation.Schedule(EveryY, (uint)size.Y);

		void EveryY(uint y)
		{
			for (int x = 0; x < size.X; x++) pass(new Int2(x, (int)y));
		}
	}

	/// <summary>
	/// Runs an <see cref="Pass1D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass1D pass, int size) => operation.Schedule(new Action<uint>(pass), (uint)size);
	
	/// <summary>
	/// Fetches a temporary <see cref="ArrayGrid{T}"/> buffer of the same size as <see cref="RenderSize"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="buffer"/>.</remarks>
	public PoolReleaseHandle FetchTemporaryBuffer(out ArrayGrid<RGB128> buffer)
	{
		buffer = null;
		var pool = temporaryBufferPool;
		
		lock (pool)
		{
			if (pool.Count > 0)
			{
				buffer = pool[^1];
				pool.RemoveAt(pool.Count - 1);
			}
		}

		buffer ??= new ArrayGrid<RGB128>(RenderSize);
		return new PoolReleaseHandle(this, buffer);
	}

	/// <summary>
	/// Fetches a temporary <see cref="SettableGrid{T}"/> buffer of the same size as <paramref name="size"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="buffer"/>.</remarks>
	public PoolReleaseHandle FetchTemporaryBuffer(out SettableGrid<RGB128> buffer, Int2 size)
	{
		var handle = FetchTemporaryBuffer(out ArrayGrid<RGB128> fetched);

		if (size == fetched.size) buffer = fetched;
		else buffer = fetched.Crop(Int2.Zero, size);

		return handle;
	}

	public delegate void Pass2D(Int2 position);
	public delegate void Pass1D(uint position);

	public readonly struct PoolReleaseHandle : IDisposable
	{
		internal PoolReleaseHandle(CompositeContext context, ArrayGrid<RGB128> buffer)
		{
			this.context = context;
			this.buffer = buffer;
		}

		readonly CompositeContext context;
		readonly ArrayGrid<RGB128> buffer;

		void IDisposable.Dispose()
		{
			var pool = context.temporaryBufferPool;
			
			lock (pool)
			{
				Ensure.IsFalse(pool.Contains(buffer));
				if (pool.Count < BufferPoolMaxCount) pool.Add(buffer);
			}
		}
	}
}