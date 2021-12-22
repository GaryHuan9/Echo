using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Pooling;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures.Grid;

namespace EchoRenderer.Rendering.PostProcessing.Operators
{
	public class GaussianBlur : IDisposable
	{
		public GaussianBlur(PostProcessingWorker worker, TextureGrid sourceBuffer, float deviation = 1f, int quality = 4)
		{
			this.worker = worker;
			this.sourceBuffer = sourceBuffer;

			this.deviation = deviation;
			this.quality = quality;

			handle = worker.FetchTemporaryBuffer(out workerBuffer, sourceBuffer.size);
		}

		readonly PostProcessingWorker worker;

		readonly TextureGrid sourceBuffer;
		readonly TextureGrid workerBuffer;

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

		Vector128<float> radiusDivisor;
		ReleaseHandle<ArrayGrid> handle;

		public void Run()
		{
			BuildRadii();

			//Run Gaussian blur passes
			for (int i = 0; i < quality; i++)
			{
				radius = radii[i];
				radiusDivisor = Vector128.Create(1f / (radius * 2f + 1f));

				worker.RunPassVertical(HorizontalBlurPass, workerBuffer);
				worker.RunPassHorizontal(VerticalBlurPass, sourceBuffer);
			}
		}

		public void Dispose() => handle.Dispose();

		void HorizontalBlurPass(int vertical)
		{
			Vector128<float> accumulator = Utilities.vector0;
			TextureGrid texture = sourceBuffer;

			for (int x = -radius; x < radius; x++) accumulator = Sse.Add(accumulator, Get(x));

			for (int x = 0; x < workerBuffer.size.x; x++)
			{
				Vector128<float> sourceHead = Get(x + radius);
				Vector128<float> sourceTail = Get(x - radius);

				accumulator = Sse.Add(accumulator, sourceHead);

				workerBuffer[new Int2(x, vertical)] = Sse.Multiply(accumulator, radiusDivisor);

				accumulator = Sse.Subtract(accumulator, sourceTail);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			Vector128<float> Get(int x) => texture[new Int2(x.Clamp(0, texture.oneLess.x), vertical)];
		}

		void VerticalBlurPass(int horizontal)
		{
			Vector128<float> accumulator = Utilities.vector0;
			TextureGrid texture = workerBuffer;

			for (int y = -radius; y < radius; y++) accumulator = Sse.Add(accumulator, Get(y));

			for (int y = 0; y < sourceBuffer.size.y; y++)
			{
				Vector128<float> sourceHead = Get(y + radius);
				Vector128<float> sourceTail = Get(y - radius);

				accumulator = Sse.Add(accumulator, sourceHead);

				sourceBuffer[new Int2(horizontal, y)] = Sse.Multiply(accumulator, radiusDivisor);

				accumulator = Sse.Subtract(accumulator, sourceTail);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			Vector128<float> Get(int y) => texture[new Int2(horizontal, y.Clamp(0, texture.oneLess.y))];
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
}