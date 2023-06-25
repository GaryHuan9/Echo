using System;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Textures;
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

	/// <inheritdoc cref="RenderTexture.TryGetLayer"/>
	public bool TryGetTexture(string label, out TextureGrid layer);

	/// <inheritdoc cref="RenderTexture.TryGetLayer"/>
	public sealed bool TryGetTexture<T>(string label, out TextureGrid<T> texture) where T : unmanaged, IColor<T>
	{
		bool found = TryGetTexture(label, out TextureGrid candidate);
		texture = found ? candidate as TextureGrid<T> : null;
		return texture != null;
	}

	/// <inheritdoc cref="RenderTexture.TryGetLayer"/>
	public sealed bool TryGetTexture<T>(string label, out SettableGrid<T> texture) where T : unmanaged, IColor<T>
	{
		bool found = TryGetTexture(label, out TextureGrid candidate);
		texture = found ? candidate as SettableGrid<T> : null;
		return texture != null;
	}

	/// <inheritdoc cref="RenderTexture.TryAddLayer"/>
	public bool TryAddTexture(string label, TextureGrid texture);

	/// <summary>
	/// Runs a <see cref="Pass2D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass2D pass, Int2 size);

	/// <summary>
	/// Runs an <see cref="Pass1D"/> on every position within <paramref name="size"/>.
	/// </summary>
	public ComputeTask RunAsync(Pass1D pass, int size);

	/// <summary>
	/// Copies the content of a <see cref="Texture"/> to a writeable <see cref="SettableGrid{T}"/>.
	/// </summary>
	public sealed ComputeTask CopyAsync<T>(Texture source, SettableGrid<T> destination) where T : unmanaged, IColor<T> =>
		RunAsync(position => destination.Set(position, source[destination.ToUV(position)].As<T>()), destination.size);

	/// <summary>
	/// Copies the content of a readable <see cref="TextureGrid{T}"/> to a writeable <see cref="SettableGrid{T}"/>.
	/// </summary>
	/// <remarks>If the type parameter of <paramref name="source"/> and <see cref="destination"/> are the same,
	/// but their <see cref="TextureGrid.size"/> is different, a resampling is performed for the best result.</remarks>
	public sealed ComputeTask CopyAsync<T>(TextureGrid<T> source, SettableGrid<T> destination) where T : unmanaged, IColor<T>
	{
		return RunAsync(source.size == destination.size ?
			position => destination.Set(position, source[position]) :
			ResamplePass, destination.size);

		void ResamplePass(Int2 position) //Performs a resampling of source from destination
		{
			Float2 uv = destination.ToUV(position);
			Float2 uvHalf = destination.ToUV(Int2.Zero);
			Float2 sourceMin = source.size * (uv - uvHalf);
			Float2 sourceMax = source.size * (uv + uvHalf);
			Int2 sourceBound = Int2.Min(source.size, sourceMax.Ceiled);

			Summation totalValue = Summation.Zero;
			Summation totalArea = Summation.Zero;

			for (int y = (int)sourceMin.Y; y < sourceBound.Y; y++)
			for (int x = (int)sourceMin.X; x < sourceBound.X; x++)
			{
				Int2 current = new Int2(x, y);
				Float2 min = Float2.Max(sourceMin, current);
				Float2 max = Float2.Min(sourceMax, current + Float2.One);
				float area = (max - min).Product;

				totalValue += source[current].ToFloat4() * area;
				totalArea += Float4.One * area;
			}

			destination.Set(position, default(T).FromFloat4(totalValue.Result / totalArea.Result));
		}
	}

	/// <summary>
	/// Asynchronously runs a Gaussian blur on a <see cref="SettableGrid{T}"/>.
	/// </summary>
	/// <returns>The actual deviation used for this blur.</returns>
	public sealed async ComputeTask<float> GaussianBlurAsync(SettableGrid<RGB128> sourceTexture, float intensity = 1f, int quality = 5)
	{
		Int2 size = sourceTexture.size;
		int[] radii = new int[quality];
		FillGaussianRadii(sourceTexture.LogSize * intensity / 256f, radii, out float actual);

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

			float deviation2 = deviation * deviation;
			int quality = radii.Length;

			int ideal = (int)MathF.Sqrt(12f * deviation2 / quality + 1f);
			if (ideal % 2 == 0) ideal--;

			float sub = quality * ideal * ideal + 4f * quality * ideal + 3f * quality;
			int threshold = ((12f * deviation2 - sub) / (-4f * ideal - 4f)).Round();

			//Record radii
			for (int i = 0; i < quality; i++) radii[i] = (i < threshold ? ideal - 1 : ideal + 1) / 2;

			//Calculate actual deviation
			actual = (quality - threshold) * (ideal + 2) * (ideal + 2) - quality;
			actual = MathF.Sqrt((threshold * ideal * ideal + actual) / 12f);
		}
	}

	/// <summary>
	/// Gets a temporary texture of the same size as <see cref="RenderSize"/>.
	/// </summary>
	/// <remarks>Used to implement <see cref="FetchTemporaryTexture(out ArrayGrid{RGB128})"/>.</remarks>
	public ArrayGrid<RGB128> RetrieveTemporaryTexture();

	/// <summary>
	/// Releases a temporary texture returned by <see cref="RetrieveTemporaryTexture"/>.
	/// </summary>
	/// <remarks>Used to implement <see cref="FetchTemporaryTexture(out ArrayGrid{RGB128})"/>.</remarks>
	public void ReleaseTemporaryTexture(ArrayGrid<RGB128> texture);

	/// <summary>
	/// Fetches a temporary <see cref="ArrayGrid{T}"/> buffer of the same size as <see cref="RenderSize"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="texture"/>.</remarks>
	public sealed PoolReleaseHandle FetchTemporaryTexture(out ArrayGrid<RGB128> texture)
	{
		texture = RetrieveTemporaryTexture();
		return new PoolReleaseHandle(this, texture);
	}

	/// <summary>
	/// Fetches a temporary <see cref="SettableGrid{T}"/> buffer of the same size as <paramref name="size"/>.
	/// Returns a handle to that buffer to be used with the `using` syntax to release the memory when done.
	/// </summary>
	/// <remarks>This method does not make any guarantee to the initial content of the <paramref name="texture"/>.</remarks>
	public sealed PoolReleaseHandle FetchTemporaryTexture(out SettableGrid<RGB128> texture, Int2 size)
	{
		PoolReleaseHandle handle = FetchTemporaryTexture(out ArrayGrid<RGB128> fetched);
		texture = size == fetched.size ? fetched : fetched.Crop(Int2.Zero, size);

		return handle;
	}

	public delegate void Pass2D(Int2 position);
	public delegate void Pass1D(uint position);

	public readonly struct PoolReleaseHandle : IDisposable
	{
		internal PoolReleaseHandle(ICompositeContext context, ArrayGrid<RGB128> texture)
		{
			this.texture = texture;
			this.context = context;
		}

		readonly ICompositeContext context;
		readonly ArrayGrid<RGB128> texture;

		void IDisposable.Dispose() => context.ReleaseTemporaryTexture(texture);
	}

	sealed class TextureNotFoundException : CompositeException
	{
		public TextureNotFoundException(string label, bool write, Type type) : base(GetMessage(label, write, type)) { }

		static string GetMessage(string label, bool write, Type type) =>
			$"No {(write ? typeof(SettableGrid<>) : typeof(TextureGrid<>)).Name} of type `{type.Name}` in {nameof(RenderTexture)} labeled as `{label}`.";
	}
}