using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Rendering.Pixels
{
	public class DirectLightingWorker : PixelWorker
	{
		public override void BeforeRender(RenderProfile profile)
		{
			SourceDistribution = new UniformDistribution(profile.TotalSample);

			for (int i = 0; i < profile.BounceLimit; i++)
			{
				foreach (Light light in profile.Scene.Lights)
				{
					int count = light.SampleCount;

					SourceDistribution.RequestSpanTwo(count); //Request span for reflectance
					SourceDistribution.RequestSpanTwo(count); //Request span for transmittance
				}
			}
		}

		public override Arena CreateArena(RenderProfile profile, uint seed)
		{
			Arena arena = base.CreateArena(profile, seed);
			arena.Random = new SystemRandom(seed);
			return arena;
		}

		public override Sample Render(Float2 uv, Arena arena)
		{
			throw new NotImplementedException();
		}

	}
}