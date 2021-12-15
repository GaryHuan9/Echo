using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceNewWorker : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			throw new NotImplementedException();
		}

		static Float3 UniformSampleOneLight(ref Interaction interaction, Arena arena)
		{
			//Handle degenerate cases
			var lights = arena.Scene.Lights;
			int lightCount = lights.Length;

			if (lightCount == 0) return Float3.zero;

			//Finds one light to sample
			Distro1 distro = arena.distribution.NextOne();
			Light source = lights[distro.Range(lightCount)];

			return lightCount * EstimateDirect(ref interaction, source, arena);
		}

		static Float3 EstimateDirect(ref Interaction interaction, Light source, Arena arena)
		{
			Assert.IsNotNull(interaction.bsdf);

			//Fetch needed stuff
			Distro2 distroLight = arena.distribution.NextTwo();
			Distro2 distroScatter = arena.distribution.NextTwo();

			Float3 radiance = Float3.zero;
			BSDF bsdf = interaction.bsdf;

			ref readonly Float3 outgoingWorld = ref interaction.outgoingWorld;

			//Sample light with multiple importance sampling
			Float3 light = source.Sample(interaction, distroLight, out Float3 incidentWorld, out float lightPDF, out float travel);

			if (lightPDF > 0f && !arena.profile.IsZero(light))
			{
				float cos = incidentWorld.Dot(interaction.normal);

				Float3 scatter = bsdf.Sample(outgoingWorld, incidentWorld) * cos;
				float pdf = bsdf.ProbabilityDensity(outgoingWorld, incidentWorld);

				if (!arena.profile.IsZero(scatter))
				{
					OccludeQuery query = interaction.SpawnOcclude(incidentWorld, travel);
					if (arena.Scene.Occlude(ref query)) light = Float3.zero;

					if (!arena.profile.IsZero(light))
					{
						float weight = source.IsDelta ? 1f : PowerHeuristic(1, lightPDF, 1, pdf);
						radiance += scatter * light * (weight / lightPDF);
					}
				}
			}

			//Sample BSDF with multiple importance sampling
			if (!source.IsDelta)
			{
				// Float3 scatter = bsdf.Sample(outgoingWorld,distroScatter,FunctionType.all, out Float3 incidentWorld, out float pdf, );
			}

			return radiance;
		}

		//No idea what this does yet but read here https://www.pbr-book.org/3ed-2018/Monte_Carlo_Integration/Importance_Sampling#PowerHeuristic
		static float PowerHeuristic(int nf, float fPdf, int ng, float gPdf)
		{
			float f = nf * fPdf, g = ng * gPdf;
			return f * f / (f * f + g * g);
		}
	}
}