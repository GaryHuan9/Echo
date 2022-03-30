using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.PostProcess;

public class OutputBarrier : PostProcessingWorker
{
	public OutputBarrier(PostProcessingEngine engine) : base(engine) { }

	public override void Dispatch() => RunPass(BarrierPass);

	unsafe void BarrierPass(Int2 position)
	{
		Vector128<float> source = PackedMath.Clamp01(renderBuffer[position]);

		float* pointer = (float*)&source;

		if (float.IsNaN(pointer[0]) || float.IsNaN(pointer[1]) || float.IsNaN(pointer[2]))
		{
			//NaN pixels are assigned an artificial magenta color
			pointer[0] = 1f;
			pointer[1] = 0f;
			pointer[2] = 1f;
		}

		pointer[3] = 1f; //Assign alpha

		renderBuffer[position] = source;
	}
}