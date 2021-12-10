using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Rendering.Pixels
{
	public class DirectLightingWorker : PixelWorker
	{
		public override Arena CreateArena(RenderProfile profile, uint seed)
		{
			Arena arena = base.CreateArena(profile, seed);
			arena.Random = new SystemRandom(seed);
			return arena;
		}

		public override Sample Render(Float2 uv, Arena arena) => Render(arena.Scene.camera.GetRay(uv), arena);

		static Float3 Render(TraceQuery query, Arena arena)
		{
			if (!arena.Scene.Trace(ref query)) return Float3.zero; //TODO: sample lights

			Interaction interaction = arena.Scene.Interact(query, out Material material);

			material.Scatter(ref interaction, arena);
			if (interaction.bsdf == null) return Render(query.Next(), arena);

			//TODO: area lights/emissive materials

			Float3 radiance = UniformSampleAllLights(interaction, arena);

			//TODO: recursively sample specular reflections and transmissions

			return radiance;
		}

		static Float3 UniformSampleAllLights(in Interaction interaction, Arena arena)
		{
			Float3 radiance = Float3.zero;

			Distribution distribution = arena.distribution;

			foreach (Light light in arena.Scene.Lights)
			{
				ReadOnlySpan<Distro2> distrosLight = distribution.NextSpanTwo();
				ReadOnlySpan<Distro2> distrosScatter = distribution.NextSpanTwo();

				int sampleCount = Math.Min(distrosLight.Length, distrosScatter.Length);

				if (sampleCount == 0)
				{
					radiance += EstimateDirect(interaction, light, distribution.NextTwo(), distribution.NextTwo(), arena);
				}
				else
				{
					Float3 single = Float3.zero;

					for (int i = 0; i < sampleCount; i++)
					{
						single += EstimateDirect(interaction, light, distrosLight[i], distrosScatter[i], arena);
					}

					radiance += single / sampleCount;
				}
			}

			return radiance;
		}

		static Float3 EstimateDirect(in Interaction interaction, Light light, in Distro2 distroLight, in Distro2 distroScatter, Arena arena)
		{
			throw new NotImplementedException();
		}

		protected override Distribution CreateDistribution(RenderProfile profile)
		{
			var distribution = new UniformDistribution(profile.TotalSample);

			for (int i = 0; i < profile.BounceLimit; i++)
			{
				foreach (Light light in profile.Scene.Lights)
				{
					int count = light.SampleCount;

					distribution.RequestSpanTwo(count); //Request span for light
					distribution.RequestSpanTwo(count); //Request span for bsdf
				}
			}

			return distribution;
		}
	}
}