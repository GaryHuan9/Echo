using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Rendering.PostProcessing.Operators;
using EchoRenderer.Textures.DimensionTwo;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class Bloom : PostProcessingWorker
	{
		public Bloom(PostProcessingEngine engine) : base(engine) => deviation = renderBuffer.LogSize / 64f;

		public float Intensity { get; set; } = 0.88f;
		public float Threshold { get; set; } = 0.95f;

		readonly float deviation;
		Array2D workerBuffer;

		public override void Dispatch()
		{
			//Allocate blur resources
			using var handle = FetchTemporaryBuffer(out workerBuffer);
			using var blur = new GaussianBlur(this, workerBuffer, deviation, 6);

			//Fill luminance threshold to workerBuffer
			RunPass(LuminancePass);

			//Run Gaussian blur on workerBuffer
			blur.Run();

			//Final pass to combine blurred workerBuffer with renderBuffer
			RunPass(CombinePass);
		}

		void LuminancePass(Int2 position)
		{
			Vector128<float> source = renderBuffer[position];
			float luminance = Utilities.GetLuminance(source);

			float brightness = luminance - Threshold;
			Vector128<float> result = Utilities.vector0;

			if (brightness > 0f && !luminance.AlmostEquals(0f))
			{
				float multiplier = brightness / luminance * Intensity;
				result = Sse.Multiply(source, Vector128.Create(multiplier));
			}

			workerBuffer[position] = result;
		}

		void CombinePass(Int2 position)
		{
			Vector128<float> source = workerBuffer[position];
			Vector128<float> target = renderBuffer[position];

			renderBuffer[position] = Sse.Add(target, source);
		}
	}
}