using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceNewWorker : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			Float3 energy = Float3.one;
			Float3 radiance = Float3.zero;

			bool specularBounce = false;

			TraceQuery query = arena.Scene.camera.GetRay(uv);

			for (int bounce = 1;; ++bounce)
			{
				bool intersected = arena.Scene.Trace(ref query);

				if (bounce == 0 || specularBounce)
				{
					//Take care of emitted light at this path vertex or from the environment
				}

				if (!intersected || bounce > arena.profile.BounceLimit) break;

				Interaction interaction = arena.Scene.Interact(query, out Material material);
				material.Scatter(ref interaction, arena);

				if (interaction.bsdf == null)
				{
					arena.allocator.Release();
					query = query.SpawnTrace();
					--bounce;
					continue;
				}

				radiance += energy * UniformSampleOneLight(interaction, arena);

				Float3 scatter = interaction.bsdf.Sample(interaction.outgoingWorld, arena.distribution.NextTwo(), out Float3 incidentWorld, out float pdf, out FunctionType sampledType);
				if (arena.profile.IsZero(scatter) || pdf <= 0f) break;

				energy *= FastMath.Abs(incidentWorld.Dot(interaction.normal)) / pdf * scatter;
				specularBounce = sampledType.Has(FunctionType.specular);

				query = query.SpawnTrace(incidentWorld);

				//Path termination with Russian Roulette

				arena.allocator.Release();
			}

			return radiance;
		}

		static Float3 UniformSampleOneLight(in Interaction interaction, Arena arena)
		{
			//Handle degenerate cases
			ReadOnlySpan<LightSource> sources = arena.Scene.LightSources;

			int count = sources.Length;
			if (count == 0) return Float3.zero;

			//Finds one light to sample
			Distro1 distro = arena.distribution.NextOne();
			LightSource source = sources[distro.Range(count)];

			return count * EstimateDirect(interaction, source, arena);
		}

		static Float3 EstimateDirect(in Interaction interaction, LightSource source, Arena arena)
		{
			//Fetch needed stuff
			Distro2 distroLight = arena.distribution.NextTwo();
			Distro2 distroScatter = arena.distribution.NextTwo();

			Float3 radiance = Float3.zero;
			BSDF bsdf = interaction.bsdf;
			Assert.IsNotNull(bsdf);

			ref readonly Float3 outgoingWorld = ref interaction.outgoingWorld;

			//Sample light with multiple importance sampling
			Float3 light = source.Sample(interaction, distroLight, out Float3 incidentWorld, out float lightPDF, out float travel);

			if (lightPDF > 0f && !arena.profile.IsZero(light))
			{
				float cos = FastMath.Abs(incidentWorld.Dot(interaction.normal));
				Float3 scatter = bsdf.Evaluate(outgoingWorld, incidentWorld) * cos;
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
				// FunctionType type = FunctionType.all;
				// Float3 scatter = bsdf.Sample(outgoingWorld, distroScatter, ref type, out incidentWorld, out float pdf);
				//
				// scatter *= FastMath.Abs(incidentWorld.Dot(interaction.normal));
				//
				// float weight = 1f;
				//
				// if (!type.HasFlag(FunctionType.specular))
				// {
				// 	lightPDF = source.ProbabilityDensity(interaction, incidentWorld);
				// 	if (lightPDF <= 0f) return radiance;
				// 	weight = PowerHeuristic(1, pdf, 1, lightPDF);
				// }
				//
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