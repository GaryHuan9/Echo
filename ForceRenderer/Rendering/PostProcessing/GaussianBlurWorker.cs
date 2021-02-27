using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.PostProcessing
{
	public class GaussianBlurWorker : PostProcessingWorker
	{
		public GaussianBlurWorker(Texture renderBuffer, float deviation) : base(renderBuffer) => this.deviation = deviation;

		readonly float deviation;

		Int2 radius;

		Texture kernel;
		Texture threshold;

		static readonly Float3 luminanceOption = new Float3(0.2126f, 0.7152f, 0.0722f);
		static readonly Vector128<float> oneVector = Vector128.Create(1f);
		static readonly Vector128<float> zeroVector = Vector128.Create(0f, 0f, 0f, 1f);

		protected override void Prepare()
		{
			radius = Int2.one * (deviation * 3f).Ceil();
			kernel = new Texture2D(radius * 2 + Int2.one);

			float alpha = -0.5f / (deviation * deviation);
			float beta = 1f / (Scalars.TAU * deviation * deviation);

			foreach (Int2 position in new EnumerableSpace2D(-radius, radius))
			{
				ref Vector128<float> target = ref kernel.GetPixel(position + radius);
				target = Vector128.Create(MathF.Exp(position.SquaredMagnitude * alpha) * beta);
			}

			threshold = new Texture2D(renderBuffer.size);

			AddPass(ThresholdPass);
			AddPass(BlurPass);
		}

		void ThresholdPass(Int2 position)
		{
			Float3 color = renderBuffer[position].XYZ;
			float brightness = color.Dot(luminanceOption);

			ref Vector128<float> target = ref threshold.GetPixel(position);
			target = brightness > 1f ? oneVector : zeroVector;
		}

		void BlurPass(Int2 position)
		{
			ref Vector128<float> sum = ref renderBuffer.GetPixel(position);

			foreach (Int2 local in new EnumerableSpace2D(-radius, radius))
			{
				Vector128<float> color = threshold.GetPixel(threshold.Restrict(position + local));
				sum = Fma.MultiplyAdd(color, kernel.GetPixel(local + radius), sum);
			}
		}
	}
}