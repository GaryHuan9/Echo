using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.PostProcess.Operators;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.PostProcess;

/// <summary>
/// Post processing depth of field effect. Faster to compute than path traced DoF.
/// </summary>
public class DepthOfField : PostProcessingWorker
{
	public DepthOfField(PostProcessingEngine engine) : base(engine) { }

	public float Intensity { get; set; } = 1f;

	public float NearStart { get; set; } = 0f;
	public float NearEnd { get; set; } = 2f;

	public float FarStart { get; set; } = 15f;
	public float FarEnd { get; set; } = 20f;

	ArrayGrid<RGB128> workerBuffer;

	MinMax nearMinMax;
	MinMax farMinMax;

	public override void Dispatch()
	{
		//Allocate resources for full resolution Gaussian blur
		float deviation = renderBuffer.LogSize / 64f * Intensity;

		using var handle = CopyTemporaryBuffer(out workerBuffer);
		using var blur = new GaussianBlur(this, workerBuffer, deviation);

		nearMinMax = new MinMax(NearStart, NearEnd);
		farMinMax = new MinMax(FarStart, FarEnd);

		blur.Run();

		RunPass(MainPass);
	}

	void MainPass(Int2 position)
	{
		throw new NotImplementedException();
		// float zDepth = renderBuffer.GetZDepth(position);
		//
		// float near = nearMinMax.InverseLerp(zDepth).Clamp();
		// float far = farMinMax.InverseLerp(zDepth).Clamp();
		//
		// Vector128<float> source = renderBuffer[position];
		// Vector128<float> worker = workerBuffer[position]; //Blurred
		//
		// var percent = Vector128.Create(CurveHelper.Sigmoid(near - far));
		// renderBuffer[position] = PackedMath.Lerp(worker, source, percent);
	}
}