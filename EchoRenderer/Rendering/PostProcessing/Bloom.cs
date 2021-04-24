using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class Bloom : PostProcessingWorker
	{
		public Bloom(PostProcessingEngine engine, float intensity, float threshold) : base(engine)
		{
			intensityVector = Vector128.Create(intensity);
			deviation = renderBuffer.size.x / 64f;
			this.threshold = threshold;
		}

		readonly Vector128<float> intensityVector;

		readonly float deviation;
		readonly float threshold;

		Texture2D workerBuffer;

		public override void Dispatch()
		{
			//Allocate blur resources
			workerBuffer = new Texture2D(renderBuffer.size);
			var blur = new GaussianBlur(this, workerBuffer)
					   {
						   Quality = 6,
						   Deviation = deviation
					   };

			//Fill luminance threshold buffer to sourceBuffer
			RunPass(LuminancePass);

			//Run Gaussian blur on sourceBuffer
			blur.Run();

			//Final pass to combine blurred sourceBuffer with renderBuffer
			RunPass(CombinePass);
		}

		void LuminancePass(Int2 position)
		{
			ref Vector128<float> source = ref renderBuffer.GetPixel(position);
			ref Vector128<float> target = ref workerBuffer.GetPixel(position);

			float luminance = Utilities.GetLuminance(source);

			if (luminance < threshold) target = Vector128<float>.Zero;
			else target = Sse.Multiply(source, intensityVector);
		}

		void CombinePass(Int2 position)
		{
			ref Vector128<float> source = ref workerBuffer.GetPixel(position);
			ref Vector128<float> target = ref renderBuffer.GetPixel(position);

			target = Sse.Add(target, source);
		}
	}
}