using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing.ToneMappers
{
	public abstract class ToneMapper : PostProcessingWorker
	{
		protected ToneMapper(PostProcessingEngine engine) : base(engine) { }

		public override void Dispatch() => RunPass(MainPass);
		protected abstract float MapLuminance(float luminance);

		void MainPass(Int2 position)
		{
			Vector128<float> source = renderBuffer[position];
			float luminance = Utilities.GetLuminance(source);

			float mapped = MapLuminance(luminance).Clamp();

			var multiplier = Vector128.Create(mapped / luminance);
			renderBuffer[position] = Sse.Multiply(source, multiplier);
		}
	}
}