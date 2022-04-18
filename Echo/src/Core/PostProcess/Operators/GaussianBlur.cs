using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using CodeHelpers.Pooling;
using Echo.Common.Mathematics.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.PostProcess.Operators;

public sealed class GaussianBlur : IDisposable
{
	public GaussianBlur(PostProcessingWorker worker, TextureGrid<RGB128> sourceBuffer, float deviation = 1f, int quality = 4)
	{
		this.worker = worker;
		this.sourceBuffer = sourceBuffer;

		this.deviation = deviation;
		this.quality = quality;

		handle = worker.FetchTemporaryBuffer(out workerBuffer, sourceBuffer.size);
	}

	readonly PostProcessingWorker worker;

	readonly TextureGrid<RGB128> sourceBuffer;
	readonly TextureGrid<RGB128> workerBuffer;

	public readonly float deviation;
	public readonly int quality;

	float _deviationActual;

	/// <summary>
	/// Because <see cref="GaussianBlur"/> uses a O(n) approximation method, this property gets
	/// the actual deviation based on <see cref="deviation"/> and <see cref="quality"/>.
	/// </summary>
	public float DeviationActual
	{
		get
		{
			BuildRadii();
			return _deviationActual;
		}
	}

	int[] radii;
	int radius;
	float diameterR;

	ReleaseHandle<ArrayGrid<RGB128>> handle;

	public void Run()
	{
		BuildRadii();

		//Run Gaussian blur passes
		for (int i = 0; i < quality; i++)
		{
			radius = radii[i];
			diameterR = 1f / (radius * 2f + 1f);

			worker.RunPassVertical(HorizontalBlurPass, workerBuffer);
			worker.RunPassHorizontal(VerticalBlurPass, sourceBuffer);
		}
	}

	public void Dispose() => handle.Dispose();

	void HorizontalBlurPass(int vertical)
	{
		var accumulator = Summation.Zero;

		for (int x = -radius; x < radius; x++) accumulator += Get(x);

		for (int x = 0; x < workerBuffer.size.X; x++)
		{
			RGB128 sourceHead = Get(x + radius);
			RGB128 sourceTail = Get(x - radius);

			accumulator += sourceHead;

			workerBuffer[new Int2(x, vertical)] = (RGB128)(accumulator.Result * diameterR);

			accumulator -= sourceTail;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		RGB128 Get(int x) => sourceBuffer[new Int2(x.Clamp(0, sourceBuffer.oneLess.X), vertical)];
	}

	void VerticalBlurPass(int horizontal)
	{
		var accumulator = Summation.Zero;

		for (int y = -radius; y < radius; y++) accumulator += Get(y);

		for (int y = 0; y < sourceBuffer.size.Y; y++)
		{
			RGB128 sourceHead = Get(y + radius);
			RGB128 sourceTail = Get(y - radius);

			accumulator += sourceHead;

			sourceBuffer[new Int2(horizontal, y)] = (RGB128)(accumulator.Result * diameterR);

			accumulator -= sourceTail;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		RGB128 Get(int y) => workerBuffer[new Int2(horizontal, y.Clamp(0, workerBuffer.oneLess.Y))];
	}

	void BuildRadii() => radii ??= BuildRadii(deviation, quality, out _deviationActual);

	static int[] BuildRadii(float deviation, int quality, out float deviationActual)
	{
		//Calculate Gaussian approximation convolution square size
		//Code based on: http://blog.ivank.net/fastest-gaussian-blur.html
		float alpha = deviation * deviation;

		int beta = MathF.Sqrt(12f * alpha / quality + 1f).Floor();
		if (beta % 2 == 0) beta--;

		float gamma = quality * beta * beta - 4f * quality * beta - 3f * quality;
		int delta = ((12f * alpha - gamma) / (-4f * beta - 4f)).Round();

		//Record radii
		int[] radii = new int[quality];

		for (int i = 0; i < quality; i++)
		{
			int size = i < delta ? beta : beta + 2;
			radii[i] = (size - 1) / 2;
		}

		//Calculate actual deviation
		deviationActual = (quality - delta) * (beta + 2) * (beta + 2) - quality;
		deviationActual = MathF.Sqrt((delta * beta * beta + deviationActual) / 12f);

		return radii;
	}
}