using System;
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

		static readonly Font font = new Font("Assets/Fonts/JetbrainsMono/FontMap.png");

		Texture sourceBuffer;
		double luminance;

		Crop2D cropSource;
		Crop2D cropTarget;

		Vector128<float> tintVector;

		const float Scale = 0.025f;
		const float Margin = 0.51f;

		const float BlurDeviation = 1.7f;
		const float BackgroundTint = 0.1f;

		const float LuminanceThreshold = 0.25f;
		const string Label = nameof(EchoRenderer);

		public override void Dispatch()
		{
			//Allocate resources for full buffer Gaussian blur
			sourceBuffer = new Texture2D(renderBuffer.size) {Wrapper = Wrapper.clamp};
			var blur = new GaussianBlur(this, sourceBuffer) {Deviation = BlurDeviation};

			//Find size and position
			float height = GetHeight();
			Float2 margin = (Float2)Margin * height;

			Float2 size = new Float2(font.GetWidth(Label.Length, height), height) + margin;
			Float2 position = renderBuffer.size.X_ + (size / 2f + margin) * new Float2(-1f, 1f);

			Int2 min = (position - size / 2f).Floored;
			Int2 max = (position + size / 2f).Ceiled + Int2.one;

			//Run watermark stamping passes
			cropSource = new Crop2D(sourceBuffer, min, max);
			cropTarget = new Crop2D(renderBuffer, min, max);

			RunCopyPass(renderBuffer, sourceBuffer); //Copies buffer
			RunPass(LuminancePass, cropSource);      //Grabs luminance

			luminance /= cropSource.size.Product;

			blur.Run(); //Run Gaussian blur

			bool lightMode = luminance > LuminanceThreshold;
			float tint = lightMode ? BackgroundTint : -BackgroundTint;

			tintVector = Vector128.Create(tint, tint, tint, 0f);

			RunPass(TintPass, cropSource); //Copies buffer

			//Write label
			Float4 fontColor = Utilities.ToColor(lightMode ? 0f : 1f);
			Font.Style style = new Font.Style(position, height, fontColor);

			font.Draw(renderBuffer, Label, style);
		}

		void LuminancePass(Int2 position)
		{
			ref Vector128<float> source = ref cropSource.GetPixel(position);
			InterlockedHelper.Add(ref luminance, Utilities.GetLuminance(source));
		}

		void TintPass(Int2 position)
		{
			ref Vector128<float> source = ref cropSource.GetPixel(position);
			ref Vector128<float> target = ref cropTarget.GetPixel(position);

			target = Sse.Add(source, tintVector);
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