using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class ToneMapping : PostProcessingWorker
	{
		public ToneMapping(PostProcessingEngine engine, float exposure, float smoothness) : base(engine)
		{
			exposureVector = Vector128.Create(exposure);
			smoothnessVector = Vector128.Create(smoothness);
		}

		readonly Vector128<float> exposureVector;
		readonly Vector128<float> smoothnessVector;

		static readonly Vector128<float> zeroVector = Vector128.Create(0f);
		static readonly Vector128<float> oneVector = Vector128.Create(1f);
		static readonly Vector128<float> halfVector = Vector128.Create(0.5f);

		public override void Dispatch() => RunPass(MainPass);

		void MainPass(Int2 position) //https://www.desmos.com/calculator/v9a3uscr8c
		{
			Vector128<float> source = Sse.Multiply(renderBuffer[position], exposureVector);
			Vector128<float> oneLess = Sse.Subtract(source, oneVector);

			Vector128<float> a = Sse.Subtract(halfVector, Sse.Multiply(halfVector, Sse.Divide(oneLess, smoothnessVector)));

			Vector128<float> h = Sse.Min(Sse.Max(a, zeroVector), oneVector);
			Vector128<float> b = Sse.Subtract(oneLess, smoothnessVector);

			renderBuffer[position] = Sse.Add(Sse.Multiply(Sse.Add(b, Sse.Multiply(smoothnessVector, h)), h), oneVector);
		}
	}
}