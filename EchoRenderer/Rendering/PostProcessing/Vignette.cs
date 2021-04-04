using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class Vignette : PostProcessingWorker
	{
		public Vignette(PostProcessingEngine engine, float intensity) : base(engine) => this.intensity = intensity;

		readonly float intensity;
		Float2 half;

		public override void Dispatch()
		{
			half = renderBuffer.size / 2f;
			RunPass(MainPass);
		}

		void MainPass(Int2 position)
		{
			ref Vector128<float> target = ref renderBuffer.GetPixel(position);

			Float2 local = (position - half) / half.MaxComponent;
			float distance = 1f - local.SquaredMagnitude * intensity;

			target = Sse.Multiply(target, Vector128.Create(distance, distance, distance, 1f));
		}
	}
}