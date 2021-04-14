using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
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

		static readonly Vector128<float> zeroVector = Vector128.Create(0f, 0f, 0f, 1f);

		public override void Dispatch()
		{
			//Allocate blur resources
			sourceBuffer = new Texture2D(renderBuffer.size);
			var blur = new GaussianBlur(this, sourceBuffer)
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
			ref Vector128<float> target = ref sourceBuffer.GetPixel(position);

			float luminance = Utilities.GetLuminance(source);
			target = luminance < threshold ? zeroVector : source;
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