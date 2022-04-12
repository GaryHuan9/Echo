using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.PostProcess.Operators;
using EchoRenderer.Core.Texturing.Grid;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.PostProcess;

/// <summary>
/// Stamps a <see cref="EchoRenderer"/> watermark on the bottom right corner of the image.
/// </summary>
public class Watermark : PostProcessingWorker
{
	public Watermark(PostProcessingEngine engine) : base(engine) { }

	CropGrid cropWorker;
	CropGrid cropTarget;

	float tint;

	static readonly Font font = Font.Find("Assets/Fonts/JetBrainsMono/FontMap.png");

	const float Scale = 0.025f;
	const float Margin = 0.51f;

	const float BlurDeviation = 0.38f;
	const float BackgroundTint = 0.2f;

	const float LuminanceThreshold = 0.35f;
	const string Label = nameof(EchoRenderer);

	public override void Dispatch()
	{
		//Find size and position
		float height = renderBuffer.LogSize * Scale;
		Font.Style style = new Font.Style(height);
		Float2 margin = (Float2)Margin * height;

		Float2 size = new Float2(font.GetWidth(Label.Length, style), style.Height) + margin;
		Float2 position = renderBuffer.size.X_ + (size / 2f + margin) * new Float2(-1f, 1f);

		Int2 min = (position - size / 2f).Floored;
		Int2 max = (position + size / 2f).Ceiled + Int2.One;

		//Allocate resources for full resolution Gaussian blur
		float deviation = height * BlurDeviation;

		using var handle = CopyTemporaryBuffer(out ArrayGrid workerBuffer);
		using var blur = new GaussianBlur(this, workerBuffer, deviation);

		//Start watermark stamping passes
		cropWorker = new CropGrid(workerBuffer, min, max);
		cropTarget = new CropGrid(renderBuffer, min, max);

		using var grab = new LuminanceGrab(this, cropWorker);

		blur.Run(); //Run Gaussian blur
		grab.Run(); //Run luminance grab

		bool lightMode = grab.Luminance > LuminanceThreshold;
		tint = lightMode ? 1f + BackgroundTint : 1f - BackgroundTint;

		RunPass(TintPass, cropWorker); //Copies buffer

		//Write label
		style = style with { Color = new RGBA128(lightMode ? 0f : 1f) };
		font.Draw(renderBuffer, Label, position, style);
	}

	void TintPass(Int2 position) => cropTarget[position] = cropWorker[position] * tint;
}