using System;
using System.Collections.Generic;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
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

	public async ComputeTask<float> GaussianBlur(SettableGrid<RGB128> sourceBuffer, float deviation = 1f, int quality = 5)
	{
		Int2 size = sourceBuffer.size;
		int[] radii = new int[quality];
		FillGaussianRadii(deviation, radii, out float actual);

		using var _ = FetchTemporaryBuffer(out var workerBuffer, size);
		Ensure.AreEqual(size, workerBuffer.size);

		int radius;
		float diameterR;

		for (int i = 0; i < quality; i++)
		{
			radius = radii[i];
			diameterR = 1f / (radius * 2 + 1);

			await RunAsync(BlurPassX, size.X); //Write to workerBuffer
			await RunAsync(BlurPassY, size.Y); //Write to sourceBuffer
		}

		return actual;

		void BlurPassX(uint x)
		{
			var accumulator = Summation.Zero;

			for (int y = -radius; y < radius; y++) accumulator += Get(y);

			for (int y = 0; y < size.Y; y++)
			{
				accumulator += Get(y + radius);

				workerBuffer.Set(new Int2((int)x, y), (RGB128)(accumulator.Result * diameterR));

				accumulator -= Get(y - radius);
			}

			RGB128 Get(int y) => sourceBuffer[new Int2((int)x, y.Clamp(0, size.Y - 1))];
		}

		void BlurPassY(uint y)
		{
			var accumulator = Summation.Zero;

			for (int x = -radius; x < radius; x++) accumulator += Get(x);

			for (int x = 0; x < size.X; x++)
			{
				accumulator += Get(x + radius);

				sourceBuffer.Set(new Int2(x, (int)y), (RGB128)(accumulator.Result * diameterR));

				accumulator -= Get(x - radius);
			}

			RGB128 Get(int x) => workerBuffer[new Int2(x.Clamp(0, size.X - 1), (int)y)];
		}

		static void FillGaussianRadii(float deviation, Span<int> radii, out float actual)
		{
			//Calculate Gaussian approximation convolution square size
			//Based on: http://blog.ivank.net/fastest-gaussian-blur.html

			float alpha = deviation * deviation;
			int quality = radii.Length;

			int beta = (int)MathF.Sqrt(12f * alpha / quality + 1f);
			if (beta % 2 == 0) beta--;

			float gamma = quality * beta * beta - 4f * quality * beta - 3f * quality;
			int threshold = ((12f * alpha - gamma) / (-4f * beta - 4f)).Round();

			//Record radii
			for (int i = 0; i < quality; i++)
			{
				int size = i < threshold ? beta : beta + 2;
				radii[i] = (size - 1) / 2;
			}

			//Calculate actual deviation
			actual = (quality - threshold) * (beta + 2) * (beta + 2) - quality;
			actual = MathF.Sqrt((threshold * beta * beta + actual) / 12f);
		}
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