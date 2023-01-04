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

/// <summary>
/// Context used by <see cref="ICompositeLayer"/>.
/// </summary>
public interface ICompositeContext
{
	/// <summary>
	/// The size of the <see cref="RenderTexture"/>.
	/// </summary>
	public Int2 RenderSize { get; }

	/// <summary>
	/// Gets a <see cref="TextureGrid{T}"/> or throws a <see cref="TextureNotFoundException"/>.
	/// </summary>
	public sealed TextureGrid<T> GetReadTexture<T>(string label) where T : unmanaged, IColor<T> =>
		TryGetTexture(label, out TextureGrid<T> texture) ? texture :
			throw new TextureNotFoundException(label, false, typeof(T));

	/// <summary>
	/// Gets a <see cref="SettableGrid{T}"/> or throws a <see cref="TextureNotFoundException"/>.
	/// </summary>
	public sealed SettableGrid<T> GetWriteTexture<T>(string label) where T : unmanaged, IColor<T> =>
		TryGetTexture(label, out SettableGrid<T> texture) ? texture :
			throw new TextureNotFoundException(label, true, typeof(T));

	/// <inheritdoc cref="RenderTexture.TryGetTexture{T, U}"/>
	public bool TryGetTexture<T>(string label, out TextureGrid<T> texture) where T : unmanaged, IColor<T>;

	/// <inheritdoc cref="RenderTexture.TryGetTexture{T, U}"/>
	public bool TryGetTexture<T>(string label, out SettableGrid<T> texture) where T : unmanaged, IColor<T>;

	/// <summary>
	/// Runs a <see cref="Pass2D"/> on every position within <see cref="RenderSize"/>.
	/// </summary>
	public sealed ComputeTask RunAsync(Pass2D pass) => RunAsync(pass, RenderSize);

	/// <summary>
	/// Runs a <see cref="Pass2D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass2D pass, Int2 size);

	/// <summary>
	/// Runs an <see cref="Pass1D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass1D pass, int size);

	/// <summary>
	/// Copies the content of a readable <see cref="TextureGrid{T}"/> to a writeable <see cref="SettableGrid{T}"/>.
	/// </summary>
	public sealed ComputeTask CopyAsync<T>(TextureGrid<T> source, SettableGrid<T> destination) where T : unmanaged, IColor<T> =>
		source.size == destination.size
			? RunAsync(position => destination.Set(position, source[position]))
			: throw new ArgumentException("Copy source and destination sizes mismatch.");

	/// <summary>
	/// Fetches a temporary <see cref="ArrayGrid{T}"/> buffer of the same size as <see cref="RenderSize"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="texture"/>.</remarks>
	public PoolReleaseHandle FetchTemporaryTexture(out ArrayGrid<RGB128> texture);

	/// <summary>
	/// Fetches a temporary <see cref="SettableGrid{T}"/> buffer of the same size as <paramref name="size"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="texture"/>.</remarks>
	public sealed PoolReleaseHandle FetchTemporaryTexture(out SettableGrid<RGB128> texture, Int2 size)
	{
		var handle = FetchTemporaryTexture(out ArrayGrid<RGB128> fetched);

		if (size == fetched.size) texture = fetched;
		else texture = fetched.Crop(Int2.Zero, size);

		return handle;
	}

	/// <summary>
	/// Asynchronously runs a Gaussian blur on a <see cref="SettableGrid{T}"/>.
	/// </summary>
	public sealed async ComputeTask<float> GaussianBlurAsync(SettableGrid<RGB128> sourceTexture, float deviation = 1f, int quality = 5)
	{
		Int2 size = sourceTexture.size;
		int[] radii = new int[quality];
		FillGaussianRadii(deviation, radii, out float actual);

		using var _ = FetchTemporaryTexture(out var workerTexture, size);
		Ensure.AreEqual(size, workerTexture.size);

		int radius;
		float diameterR;

		for (int i = 0; i < quality; i++)
		{
			radius = radii[i];
			diameterR = 1f / (radius * 2 + 1);

			await RunAsync(BlurPassX, size.X); //Write to workerTexture
			await RunAsync(BlurPassY, size.Y); //Write to sourceTexture
		}

		return actual;

		void BlurPassX(uint x)
		{
			var accumulator = Summation.Zero;

			for (int y = -radius; y < radius; y++) accumulator += Get(y);

			for (int y = 0; y < size.Y; y++)
			{
				accumulator += Get(y + radius);

				workerTexture.Set(new Int2((int)x, y), (RGB128)Float4.Max(accumulator.Result * diameterR, Float4.Zero));

				accumulator -= Get(y - radius);
			}

			RGB128 Get(int y) => sourceTexture[new Int2((int)x, y.Clamp(0, size.Y - 1))];
		}

		void BlurPassY(uint y)
		{
			var accumulator = Summation.Zero;

			for (int x = -radius; x < radius; x++) accumulator += Get(x);

			for (int x = 0; x < size.X; x++)
			{
				accumulator += Get(x + radius);

				sourceTexture.Set(new Int2(x, (int)y), (RGB128)Float4.Max(accumulator.Result * diameterR, Float4.Zero));

				accumulator -= Get(x - radius);
			}

			RGB128 Get(int x) => workerTexture[new Int2(x.Clamp(0, size.X - 1), (int)y)];
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
		internal PoolReleaseHandle(List<ArrayGrid<RGB128>> pool, ArrayGrid<RGB128> buffer)
		{
			this.pool = pool;
			this.buffer = buffer;
		}

		readonly List<ArrayGrid<RGB128>> pool;
		readonly ArrayGrid<RGB128> buffer;

		const int TexturePoolMaxCount = 8;

		void IDisposable.Dispose()
		{
			lock (pool)
			{
				Ensure.IsFalse(pool.Contains(buffer));
				if (pool.Count < TexturePoolMaxCount) pool.Add(buffer);
			}
		}
	}

	sealed class TextureNotFoundException : ICompositeLayer.CompositeException
	{
		public TextureNotFoundException(string label, bool write, Type type) : base(GetMessage(label, write, type)) { }

		static string GetMessage(string label, bool write, Type type) =>
			$"No {(write ? typeof(SettableGrid<>) : typeof(TextureGrid<>))} of type `{type}` in {nameof(RenderTexture)} labeled as `{label}`.";
	}
}