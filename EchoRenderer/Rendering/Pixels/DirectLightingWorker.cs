using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Sampling;

namespace EchoRenderer.Rendering.Pixels
{
	public class DirectLightingWorker : PixelWorker
	{
		Distribution sourceDistribution;

		public override void BeforeRender(RenderProfile profile)
		{
			base.BeforeRender(profile);

			sourceDistribution = new UniformDistribution(profile.BaseSample);

			for (int i = 0; i < profile.BounceLimit; i++)
			{
				foreach (Light light in profile.Scene.Lights)
				{
					int count = light.SampleCount;

					sourceDistribution.RequestSpanTwo(count);
					sourceDistribution.RequestSpanTwo(count);
				}
			}
		}

		public override Arena CreateArena(RenderProfile profile, uint seed) => new(profile)
																			   {
																				   Distribution = sourceDistribution,
																				   Random = new SystemRandom(seed)
																			   };

		public override Sample Render(Float2 uv, Arena arena)
		{
			throw new NotImplementedException();
		}
	}
}