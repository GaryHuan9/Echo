using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class GaussianBlur
	{
		public GaussianBlur(PostProcessingWorker worker, Texture2D sourceBuffer)
		{
			this.worker = worker;
			this.sourceBuffer = sourceBuffer;

			workerBuffer = new Array2D(sourceBuffer.size) {Wrapper = Wrappers.clamp};
		}

		public float Deviation { get; set; }
		public int Quality { get; set; } = 4;

		float _deviationActual;

		/// <summary>
		/// Because <see cref="GaussianBlur"/> uses a O(n) approximation method, this property gets
		/// the actual deviation based on <see cref="Deviation"/> and <see cref="Quality"/>.
		/// </summary>
		public float DeviationActual
		{
			get
			{
				BuildRadii();
				return _deviationActual;
			}
			private set => _deviationActual = value;
		}

		/// <summary>
		/// Can be used to change the <see cref="Texture.Wrapper"/> to change how
		/// the blur will be calculated on the border of the texture.
		/// </summary>
		public IWrapper Wrapper
		{
			get => workerBuffer.Wrapper;
			set => workerBuffer.Wrapper = value;
		}

		readonly PostProcessingWorker worker;
		readonly Texture2D sourceBuffer;
		readonly Texture2D workerBuffer;

		int[] radii = Array.Empty<int>();
		float builtDeviation;

		public void Run()
		{
			BuildRadii();

			using var _ = new ScopedWrapper(sourceBuffer, Wrapper);

			//Run Gaussian blur passes
			for (int i = 0; i < Quality; i++)
			{
				int radius = radii[i];

				worker.RunPassHorizontal(vertical => HorizontalBlurPass(vertical, radius), workerBuffer);
				worker.RunPassVertical(horizontal => VerticalBlurPass(horizontal, radius), sourceBuffer);
			}
		}

		void BuildRadii()
		{
			if (radii.Length == Quality && builtDeviation.AlmostEquals(Deviation)) return;

			Array.Resize(ref radii, Quality);
			builtDeviation = Deviation;

			//Calculate Gaussian approximation convolution square size
			//Code based on: http://blog.ivank.net/fastest-gaussian-blur.html
			float alpha = Deviation * Deviation;

			int beta = MathF.Sqrt(12f * alpha / Quality + 1f).Floor();
			if (beta % 2 == 0) beta--;

			float gamma = Quality * beta * beta - 4f * Quality * beta - 3f * Quality;
			int delta = ((12f * alpha - gamma) / (-4f * beta - 4f)).Round();

			//Record radii
			for (int i = 0; i < Quality; i++)
			{
				int size = i < delta ? beta : beta + 2;
				radii[i] = (size - 1) / 2;
			}

			//Calculate actual deviation
			DeviationActual = MathF.Sqrt((delta * beta * beta + (Quality - delta) * (beta + 2) * (beta + 2) - Quality) / 12f);
		}

		void HorizontalBlurPass(int vertical, int radius)
		{
			Vector128<float> accumulator = Vector128<float>.Zero;
			Vector128<float> divisor = Vector128.Create(1f / (radius * 2f + 1f));

			for (int x = -radius; x < radius; x++) accumulator = Sse.Add(accumulator, Get(x));

			for (int x = 0; x < workerBuffer.size.x; x++)
			{
				Vector128<float> sourceHead = Get(x + radius);
				Vector128<float> sourceTail = Get(x - radius);

				accumulator = Sse.Add(accumulator, sourceHead);

				workerBuffer[new Int2(x, vertical)] = Sse.Multiply(accumulator, divisor);

				accumulator = Sse.Subtract(accumulator, sourceTail);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			Vector128<float> Get(int x) => sourceBuffer[new Int2(x.Clamp(0, sourceBuffer.oneLess.x), vertical)];
		}

		void VerticalBlurPass(int horizontal, int radius)
		{
			Vector128<float> accumulator = Vector128<float>.Zero;
			Vector128<float> divisor = Vector128.Create(1f / (radius * 2f + 1f));

			for (int y = -radius; y < radius; y++) accumulator = Sse.Add(accumulator, Get(y));

			for (int y = 0; y < sourceBuffer.size.y; y++)
			{
				Vector128<float> sourceHead = Get(y + radius);
				Vector128<float> sourceTail = Get(y - radius);

				accumulator = Sse.Add(accumulator, sourceHead);

				sourceBuffer[new Int2(horizontal, y)] = Sse.Multiply(accumulator, divisor);

				accumulator = Sse.Subtract(accumulator, sourceTail);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			Vector128<float> Get(int y) => workerBuffer[new Int2(horizontal, y.Clamp(0, workerBuffer.oneLess.y))];
		}
	}
}