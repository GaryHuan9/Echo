using System.Runtime.CompilerServices;
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

			for (int bounce = 0;; bounce++)
			{
				bool intersected = arena.Scene.Trace(ref query);

				if (bounce == 0 || specularBounce)
				{
					//TODO: Take care of emitted light at this path vertex or from the environment

					if (intersected)
					{

					}
					else
					{
						foreach (AmbientLight ambient in arena.Scene.AmbientSources)
						{
							radiance += ambient.Evaluate(query.ray);
						}
					}
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
				specularBounce = sampledType.Any(FunctionType.specular);

				query = query.SpawnTrace(incidentWorld);

				//TODO: Path termination with Russian Roulette

				arena.allocator.Release();
			}

			return radiance;
		}

		static Float3 UniformSampleOneLight(in Interaction interaction, Arena arena)
		{
			//Handle degenerate cases
			var sources = arena.Scene.LightSources;

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

			//Sample light with importance sampling
			Float3 light = source.Sample(interaction, distroLight, out Float3 incidentWorld, out float pdfLight, out float travel);

			if (pdfLight > 0f && !arena.profile.IsZero(light))
			{
				Float3 scatter = bsdf.Evaluate(outgoingWorld, incidentWorld);
				float pdf = bsdf.ProbabilityDensity(outgoingWorld, incidentWorld);

				scatter *= FastMath.Abs(incidentWorld.Dot(interaction.normal));

				if (!arena.profile.IsZero(scatter))
				{
					OccludeQuery query = interaction.SpawnOcclude(incidentWorld, travel);
					if (arena.Scene.Occlude(ref query)) light = Float3.zero;

					if (!arena.profile.IsZero(light))
					{
						float weight = source.type.IsDelta() ? 1f : PowerHeuristic(pdf, pdfLight);
						radiance += scatter * light * (weight / pdfLight);
					}
				}
			}

			//Sample BSDF with importance sampling
			if (!source.type.IsDelta())
			{
				Float3 scatter = bsdf.Sample(outgoingWorld, distroScatter, out incidentWorld, out float pdf, out FunctionType sampledType);

				scatter *= FastMath.Abs(incidentWorld.Dot(interaction.normal));

				float weight = 1f;

				if (!sampledType.Any(FunctionType.specular))
				{
					pdfLight = source.ProbabilityDensity(interaction, incidentWorld);
					if (pdfLight <= 0f) return radiance;
					weight = PowerHeuristic(pdf, pdfLight);
				}

				TraceQuery query = interaction.SpawnTrace(incidentWorld);

				if (arena.Scene.Trace(ref query))
				{
					//TODO: evaluate light at intersection if area light is our source
				}
				else
				{
					//TODO: evaluate infinite lights
				}
			}

			return radiance;
		}

		// static Float3 SampleLight()
		// {
		// 	Float3 light = source.Sample(interaction, distroLight, out Float3 incidentWorld, out float pdfLight, out float travel);
		//
		// 	if (pdfLight > 0f && !arena.profile.IsZero(light))
		// 	{
		// 		float cos = FastMath.Abs(incidentWorld.Dot(interaction.normal));
		// 		Float3 scatter = bsdf.Evaluate(outgoingWorld, incidentWorld) * cos;
		//
		// 		if (!arena.profile.IsZero(scatter))
		// 		{
		// 			OccludeQuery query = interaction.SpawnOcclude(incidentWorld, travel);
		// 			if (arena.Scene.Occlude(ref query)) light = Float3.zero;
		//
		// 			if (!arena.profile.IsZero(light))
		// 			{
		// 				float pdf = bsdf.ProbabilityDensity(outgoingWorld, incidentWorld);
		// 				float weight = delta ? 1f : PowerHeuristic(pdf, pdfLight);
		// 				radiance += scatter * light * (weight / pdfLight);
		// 			}
		// 		}
		// 	}
		// }

		/// <summary>
		/// Power heuristic with a constant power of two used for multiple importance sampling
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float PowerHeuristic(float pdf0, float pdf1, int count0 = 1, int count1 = 1)
		{
			float product0 = pdf0 * count0;
			float product1 = pdf1 * count1;

			return product0 * product0 / (product0 * product0 + product1 * product1);
		}
	}
}