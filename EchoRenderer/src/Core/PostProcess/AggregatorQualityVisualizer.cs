﻿using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using CodeHelpers.Threads;
using EchoRenderer.Common;
using EchoRenderer.Core.Texturing;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.PostProcess;

public class AggregatorQualityVisualizer : PostProcessingWorker
{
	public AggregatorQualityVisualizer(PostProcessingEngine engine) : base(engine)
	{
		ReadOnlySpan<Float4> colors = stackalloc Float4[]
		{
			Utilities.ToColor("#000000"),
			Utilities.ToColor("#0000FF"),
			Utilities.ToColor("#00FFFF"),
			Utilities.ToColor("#00FF00"),
			Utilities.ToColor("#FFFF00"),
			Utilities.ToColor("#FF0000"),
			Utilities.ToColor("#FFFFFF")
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
		var style = new Font.Style(height, Float4.One);

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
		Float4 source = Utilities.ToFloat4(renderBuffer[position]);

		InterlockedHelper.Max(ref maxCost, source.X);
		InterlockedHelper.Max(ref totalCost, source.Y);
		InterlockedHelper.Max(ref totalSample, source.Z);
	}

	void MainPass(Int2 position)
	{
		float percent = renderBuffer[position].GetElement(0) / maxCost;
		renderBuffer[position] = Utilities.ToVector(costGradient[percent]);
	}
}