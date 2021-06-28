using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using static EchoRenderer.Mathematics.Utilities;

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
			Float2 local = (position - half) / half.MaxComponent;
			float distance = 1f - local.SquaredMagnitude * intensity;

			Vector128<float> target = Clamp(vector0, vector1, renderBuffer[position]);
			renderBuffer[position] = Sse.Multiply(target, Vector128.Create(distance));

			//NOTE: We can use a little bit of film grain (noise) to remove the banding
		}
	}
}