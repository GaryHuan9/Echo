using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class OutputBarrier : PostProcessingWorker
	{
		public OutputBarrier(PostProcessingEngine engine) : base(engine) { }

		static readonly Vector128<float> vector0 = Vector128.Create(0f);
		static readonly Vector128<float> vector1 = Vector128.Create(1f);

		public override void Dispatch() => RunPass(BarrierPass);

		unsafe void BarrierPass(Int2 position)
		{
			Vector128<float> source = Sse.Min(Sse.Max(renderBuffer[position], vector0), vector1);

			float* pointer = (float*)&source;
			*(pointer + 3) = 1f; //Assign alpha

			renderBuffer[position] = source;
		}
	}
}