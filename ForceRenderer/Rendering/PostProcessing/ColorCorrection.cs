﻿using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Rendering.PostProcessing
{
	public class ColorCorrection : PostProcessingWorker
	{
		public ColorCorrection(PostProcessingEngine engine, float smoothness) : base(engine) => smoothnessVector = Vector128.Create(smoothness);

		readonly Vector128<float> smoothnessVector;

		static readonly Vector128<float> zeroVector = Vector128.Create(0f);
		static readonly Vector128<float> oneVector = Vector128.Create(1f);
		static readonly Vector128<float> halfVector = Vector128.Create(0.5f);

		public override void Dispatch()
		{
			RunPass(GammaCorrectPass);
		}

		unsafe void GammaCorrectPass(Int2 position) //https://www.desmos.com/calculator/v9a3uscr8c
		{
			ref Vector128<float> target = ref renderBuffer.GetPixel(position);

			Vector128<float> a = Sse.Divide(Sse.Subtract(target, oneVector), smoothnessVector);
			Vector128<float> h = Sse.Subtract(halfVector, Sse.Multiply(halfVector, a));

			h = Sse.Min(Sse.Max(h, zeroVector), oneVector);

			Vector128<float> b = Sse.Subtract(Sse.Subtract(target, oneVector), smoothnessVector);
			Vector128<float> result = Sse.Add(Sse.Multiply(Sse.Add(b, Sse.Multiply(smoothnessVector, h)), h), oneVector);

			*((float*)&result + 3) = 1f; //Set alpha to one
			target = result;
		}
	}
}