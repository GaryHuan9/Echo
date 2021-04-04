using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class Bloom : PostProcessingWorker
	{
		public Bloom(PostProcessingEngine engine, float strength = 1f, float threshold = 1f) : base(engine)
		{
			deviation = strength * renderBuffer.size.x / 60f;
			this.threshold = threshold;
		}

		readonly float deviation;
		readonly float threshold;

		Texture sourceBuffer;
		Texture targetBuffer;

		static readonly Vector128<float> luminanceOption = Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);
		static readonly Vector128<float> zeroVector = Vector128.Create(0f, 0f, 0f, 1f);

		public override void Dispatch()
		{
			//Allocate blur buffers; clamp is needed for outside pixels
			sourceBuffer = new Texture2D(renderBuffer.size) {Wrapper = Wrapper.clamp};
			targetBuffer = new Texture2D(renderBuffer.size) {Wrapper = Wrapper.clamp};

			//Create luminance threshold buffer
			RunPass(LuminancePass);

			//Calculate Gaussian approximation convolution square size
			//Code based on: http://blog.ivank.net/fastest-gaussian-blur.html
			float alpha = deviation * deviation;
			const float Pass = 6;

			int beta = MathF.Sqrt(12f * alpha / Pass + 1f).Floor();
			if (beta % 2 == 0) beta--;

			float gamma = Pass * beta * beta - 4f * Pass * beta - 3f * Pass;
			int delta = ((12f * alpha - gamma) / (-4f * beta - 4f)).Round();

			//Run Gaussian blur passes
			for (int i = 0; i < Pass; i++)
			{
				int size = i < delta ? beta : beta + 2;
				int radius = (size - 1) / 2;

				RunPassHorizontal(vertical => HorizontalBlurPass(vertical, radius));
				RunPassVertical(horizontal => VerticalBlurPass(horizontal, radius));
			}

			//Final pass to combine blurred image with render buffer
			RunPass(CombinePass);
		}

		unsafe void LuminancePass(Int2 position)
		{
			ref Vector128<float> source = ref renderBuffer.GetPixel(position);
			ref Vector128<float> target = ref sourceBuffer.GetPixel(position);

			Vector128<float> single = Sse41.DotProduct(source, luminanceOption, 0b1110_0001);
			target = *(float*)&single < threshold ? zeroVector : source;
		}

		void HorizontalBlurPass(int vertical, int radius)
		{
			Vector128<float> accumulator = Vector128<float>.Zero;
			Vector128<float> divisor = Vector128.Create(1f / (radius * 2f + 1f));

			for (int x = -radius; x < radius; x++)
			{
				ref readonly Vector128<float> source = ref sourceBuffer.GetPixel(new Int2(x, vertical));
				accumulator = Sse.Add(accumulator, source);
			}

			for (int x = 0; x < renderBuffer.size.x; x++)
			{
				ref readonly Vector128<float> sourceHead = ref sourceBuffer.GetPixel(new Int2(x + radius, vertical));
				ref readonly Vector128<float> sourceTail = ref sourceBuffer.GetPixel(new Int2(x - radius, vertical));

				ref var target = ref targetBuffer.GetPixel(new Int2(x, vertical));

				accumulator = Sse.Add(accumulator, sourceHead);
				target = Sse.Multiply(accumulator, divisor);
				accumulator = Sse.Subtract(accumulator, sourceTail);
			}
		}

		void VerticalBlurPass(int horizontal, int radius)
		{
			Vector128<float> accumulator = Vector128<float>.Zero;
			Vector128<float> divisor = Vector128.Create(1f / (radius * 2f + 1f));

			for (int y = -radius; y < radius; y++)
			{
				ref readonly Vector128<float> source = ref targetBuffer.GetPixel(new Int2(horizontal, y));
				accumulator = Sse.Add(accumulator, source);
			}

			for (int y = 0; y < renderBuffer.size.y; y++)
			{
				ref readonly Vector128<float> sourceHead = ref targetBuffer.GetPixel(new Int2(horizontal, y + radius));
				ref readonly Vector128<float> sourceTail = ref targetBuffer.GetPixel(new Int2(horizontal, y - radius));

				ref var target = ref sourceBuffer.GetPixel(new Int2(horizontal, y));

				accumulator = Sse.Add(accumulator, sourceHead);
				target = Sse.Multiply(accumulator, divisor);
				accumulator = Sse.Subtract(accumulator, sourceTail);
			}
		}

		unsafe void CombinePass(Int2 position)
		{
			ref Vector128<float> source = ref sourceBuffer.GetPixel(position);
			ref Vector128<float> target = ref renderBuffer.GetPixel(position);

			Vector128<float> result = Sse.Add(target, source);
			*((float*)&result + 3) = 1f; //Assign alpha

			target = result;
		}
	}
}