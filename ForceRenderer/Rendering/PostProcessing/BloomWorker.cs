using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.PostProcessing
{
	public class BloomWorker : PostProcessingWorker
	{
		public BloomWorker(Texture renderBuffer, float strength = 1f, float threshold = 1f) : base(renderBuffer)
		{
			deviation = strength * renderBuffer.size.x / 60f;
			this.threshold = threshold;
		}

		readonly float deviation;
		readonly float threshold;

		Int2 radius;

		Texture kernel;
		Texture bloom;

		// Texture source;
		// Texture target;

		static readonly Vector128<float> luminanceOption = Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);
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

			bloom = new Texture2D(renderBuffer.size);

			AddPass(LuminancePass);
			AddPass(BlurPass);
		}

		unsafe void LuminancePass(Int2 position)
		{
			ref Vector128<float> source = ref renderBuffer.GetPixel(position);
			ref Vector128<float> target = ref bloom.GetPixel(position);

			Vector128<float> single = Sse41.DotProduct(source, luminanceOption, 0b1110_0001);
			target = *(float*)&single < threshold ? zeroVector : source;
		}

		void BlurPass(Int2 position)
		{
			ref Vector128<float> sum = ref renderBuffer.GetPixel(position);

			foreach (Int2 local in new EnumerableSpace2D(-radius, radius))
			{
				Vector128<float> color = bloom.GetPixel(bloom.Restrict(position + local));
				sum = Fma.MultiplyAdd(color, kernel.GetPixel(local + radius), sum);
			}
		}
	}
}