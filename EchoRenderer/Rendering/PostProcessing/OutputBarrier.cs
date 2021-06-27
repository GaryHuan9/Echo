using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using static EchoRenderer.Mathematics.Utilities;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class OutputBarrier : PostProcessingWorker
	{
		public OutputBarrier(PostProcessingEngine engine) : base(engine) { }

		public override void Dispatch() => RunPass(BarrierPass);

		unsafe void BarrierPass(Int2 position)
		{
			Vector128<float> source = Clamp(vector0, vector1, renderBuffer[position]);

			float* pointer = (float*)&source;
			*(pointer + 3) = 1f; //Assign alpha

			renderBuffer[position] = source;
		}
	}
}