using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.PostProcess.Operators;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.PostProcess;

public class Bloom : PostProcessingWorker
{
	public Bloom(PostProcessingEngine engine) : base(engine) => deviation = renderBuffer.LogSize / 64f;

	public float Intensity { get; set; } = 0.88f;
	public float Threshold { get; set; } = 0.95f;

	readonly float deviation;
	ArrayGrid workerBuffer;

	public override void Dispatch()
	{
		//Allocate blur resources
		using var handle = FetchTemporaryBuffer(out workerBuffer);
		using var blur = new GaussianBlur(this, workerBuffer, deviation, 6);

		//Fill luminance threshold to workerBuffer
		RunPass(LuminancePass);

		//Run Gaussian blur on workerBuffer
		blur.Run();

		//Final pass to combine blurred workerBuffer with renderBuffer
		RunPass(CombinePass);
	}

	void LuminancePass(Int2 position)
	{
		RGB128 source = renderBuffer[position];
		float luminance = source.Luminance;

		if (luminance > Threshold)
		{
			float excess = (luminance - Threshold) / luminance;
			workerBuffer[position] = source * excess * Intensity;
		}
		else workerBuffer[position] = RGB128.Black;
	}

	void CombinePass(Int2 position)
	{
		RGB128 source = workerBuffer[position];
		RGB128 target = renderBuffer[position];
		renderBuffer[position] = target + source;
	}
}