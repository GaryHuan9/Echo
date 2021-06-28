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

			Filter(0);
			Filter(1);
			Filter(2);

			pointer[3] = 1f; //Assign alpha

			renderBuffer[position] = source;

			void Filter(int index)
			{
				ref float value = ref pointer[index];
				value = float.IsNaN(value) ? 0f : value;
			}
		}
	}
}