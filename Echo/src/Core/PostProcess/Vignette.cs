﻿using CodeHelpers.Packed;
using Echo.Common.Mathematics.Randomization;

namespace Echo.Core.PostProcess;

public class Vignette : PostProcessingWorker
{
	public Vignette(PostProcessingEngine engine) : base(engine) { }

	public float Intensity { get; set; } = 0.57f;
	public float FilmGrain { get; set; } = 0.01f; //A little bit of film grain helps with the color banding

	public override void Dispatch() => RunPassHorizontal(HorizontalPass);

	void HorizontalPass(int horizontal)
	{
		Prng random = new SystemPrng((uint)horizontal);

		for (int y = 0; y < renderBuffer.size.Y; y++)
		{
			Int2 position = new Int2(horizontal, y);
			Float2 uv = position * renderBuffer.sizeR;

			float distance = (uv - Float2.Half).SquaredMagnitude * Intensity;
			float multiplier = random.Next1(-FilmGrain, FilmGrain) - distance;

			renderBuffer[position] *= 1f + multiplier;
		}
	}
}