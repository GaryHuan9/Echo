using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using CodeHelpers.Threads;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.InOut;

namespace Echo.Core.PostProcess;

public class AggregatorQualityVisualizer : PostProcessingWorker
{
	public AggregatorQualityVisualizer(PostProcessingEngine engine) : base(engine)
	{
		ReadOnlySpan<RGBA128> colors = stackalloc RGBA128[]
		{
			RGBA128.Parse("#000000"),
			RGBA128.Parse("#0000FF"),
			RGBA128.Parse("#00FFFF"),
			RGBA128.Parse("#00FF00"),
			RGBA128.Parse("#FFFF00"),
			RGBA128.Parse("#FF0000"),
			RGBA128.Parse("#FFFFFF")
		};

		costGradient = new Gradient();

		for (int i = 0; i < colors.Length; i++)
		{
			float percent = i / (colors.Length - 1f);
			costGradient.Add(percent, colors[i]);
		}
	}

	readonly Gradient costGradient;

	float maxCost;
	float totalCost;
	float totalSample;

	static readonly Font font = Font.Find("Assets/Fonts/JetBrainsMono/FontMap.png");

	const float Scale = 0.03f;

	public override void Dispatch()
	{
		RunPass(GatherPass);
		RunPass(MainPass);

		float max = InterlockedHelper.Read(ref maxCost);
		float cost = InterlockedHelper.Read(ref totalCost);
		float sample = InterlockedHelper.Read(ref totalSample);

		string[] labels =
		{
			$"Total {cost:N0}",
			$"Average {cost / sample:F2}",
			$"Max {max:N0}"
		};

		float height = renderBuffer.size.Y * Scale;
		var style = new Font.Style(height);

		for (int i = 0; i < labels.Length; i++)
		{
			string label = labels[i];
			float width = font.GetWidth(label.Length, style);

			Float2 position = new Float2(height + width / 2f, height * (i + 1.5f));
			font.Draw(renderBuffer, label, position, style);
		}
	}

	void GatherPass(Int2 position)
	{
		Float4 source = renderBuffer[position];

		InterlockedHelper.Max(ref maxCost, source.X);
		InterlockedHelper.Max(ref totalCost, source.Y);
		InterlockedHelper.Max(ref totalSample, source.Z);
	}

	void MainPass(Int2 position)
	{
		float percent = ((Float4)renderBuffer[position]).X / maxCost;
		renderBuffer[position] = (RGB128)costGradient[percent.Clamp()];
	}
}