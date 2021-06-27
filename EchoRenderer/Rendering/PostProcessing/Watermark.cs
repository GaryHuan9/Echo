﻿using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.IO;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	/// <summary>
	/// Stamps a <see cref="EchoRenderer"/> watermark on the bottom right corner of the image.
	/// </summary>
	public class Watermark : PostProcessingWorker
	{
		public Watermark(PostProcessingEngine engine) : base(engine) { }

		Crop2D cropWorker;
		Crop2D cropTarget;

		double luminanceAverage;
		Vector128<float> tintVector;

		static readonly Font font = Font.Find("Assets/Fonts/JetBrainsMono/FontMap.png");

		const float Scale = 0.025f;
		const float Margin = 0.51f;

		const float BlurDeviation = 1.7f;
		const float BackgroundTint = 0.1f;

		const float LuminanceThreshold = 0.25f;
		const string Label = nameof(EchoRenderer);

		public override void Dispatch()
		{
			//Allocate resources for full buffer Gaussian blur
			using var handle = FetchTemporaryBuffer(out Array2D workerBuffer);
			using var blur = new GaussianBlur(this, workerBuffer, BlurDeviation);

			//Find size and position
			Font.Style style = new Font.Style(GetHeight());
			Float2 margin = (Float2)Margin * style.Height;

			Float2 size = new Float2(font.GetWidth(Label.Length, style), style.Height) + margin;
			Float2 position = renderBuffer.size.X_ + (size / 2f + margin) * new Float2(-1f, 1f);

			Int2 min = (position - size / 2f).Floored;
			Int2 max = (position + size / 2f).Ceiled + Int2.one;

			//Run watermark stamping passes
			cropWorker = new Crop2D(workerBuffer, min, max);
			cropTarget = new Crop2D(renderBuffer, min, max);

			RunCopyPass(renderBuffer, workerBuffer); //Copies buffer
			RunPass(LuminancePass, cropWorker);      //Grabs luminance

			luminanceAverage /= cropWorker.size.Product;

			blur.Run(); //Run Gaussian blur

			bool lightMode = luminanceAverage > LuminanceThreshold;
			float tint = lightMode ? 1f + BackgroundTint : 1f - BackgroundTint;

			tintVector = Vector128.Create(tint);
			RunPass(TintPass, cropWorker); //Copies buffer

			//Write label
			style = style with {Color = Utilities.ToColor(lightMode ? 0f : 1f)};
			font.Draw(renderBuffer, Label, position, style);
		}

		void LuminancePass(Int2 position)
		{
			float luminance = Utilities.GetLuminance(cropWorker[position]);
			InterlockedHelper.Add(ref luminanceAverage, luminance);
		}

		void TintPass(Int2 position)
		{
			Vector128<float> source = cropWorker[position];
			cropTarget[position] = Sse.Multiply(source, tintVector);
		}

		float GetHeight()
		{
			Float2 scale = renderBuffer.size * Scale;

			//Using log to scale because the average is nicer
			float logWidth = MathF.Log(scale.x);
			float logHeight = MathF.Log(scale.y);

			return MathF.Exp((logWidth + logHeight) / 2f);
		}
	}
}