using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
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

					if (intersected) { }
					else
					{
						foreach (AmbientLight ambient in arena.Scene.AmbientSources)
						{
							radiance += energy * ambient.Evaluate(query.ray.direction);
						}
					}
				}

				if (!intersected || bounce > arena.profile.BounceLimit) break;

				using var _ = arena.allocator.Begin();

				Interaction interaction = arena.Scene.Interact(query);
				interaction.shade.material.Scatter(ref interaction, arena);

				if (interaction.bsdf == null)
				{
					query = query.SpawnTrace();
					--bounce;
					continue;
				}

				if (interaction.bsdf.Count() == 0)
				{
					energy = Float3.zero;
					break;
				}

				Float3 scatter = interaction.bsdf.Sample
				(
					interaction.outgoing, arena.distribution.NextTwo(),
					out Float3 incident, out float pdf, out FunctionType sampledType
				);

				//TODO: this is a mess

				if (!scatter.PositiveRadiance() || !FastMath.Positive(pdf)) break;

				Float3 light = ImportanceSampleOneLight(interaction, out LightSource source, arena);
				radiance += energy * (light + ImportanceSampleBSDF(interaction, source, arena));

				energy *= interaction.NormalDot(incident) / pdf * scatter;
				specularBounce = sampledType.Any(FunctionType.specular);

				if (!energy.PositiveRadiance()) break;

				query = query.SpawnTrace(incident);

				//TODO: Path termination with Russian Roulette
			}

			return radiance;
		}

		/// <summary>
		/// Importance samples one <see cref="LightSource"/> in <paramref name="arena.Scene"/> and returns its radiance.
		/// </summary>
		static Float3 ImportanceSampleOneLight(in Interaction interaction, out LightSource source, Arena arena)
		{
			//Handle degenerate cases
			var sources = arena.Scene.LightSources;
			int length = sources.Length;

			if (length == 0)
			{
				source = null;
				return Float3.zero;
			}

			//Finds one light to sample
			source = sources[arena.distribution.NextOne().Range(length)];
			return length * ImportanceSampleOneLight(interaction, source, arena);
		}

		/// <summary>
		/// Importance samples <paramref name="source"/> at <paramref name="interaction"/> and returns the combined radiance.
		/// </summary>
		static Float3 ImportanceSampleOneLight(in Interaction interaction, LightSource source, Arena arena)
		{
			//Sample light source
			Float3 light = source.Sample
			(
				interaction, arena.distribution.NextTwo(),
				out Float3 incident, out float pdf, out float travel
			);

			if (!FastMath.Positive(pdf) || !light.PositiveRadiance()) return Float3.zero;

			//Evaluate bsdf at light source's directions
			ref readonly Float3 outgoing = ref interaction.outgoing;
			Float3 scatter = interaction.bsdf.Evaluate(outgoing, incident);
			scatter *= interaction.NormalDot(incident);

			//Conditionally terminate if radiance is non-positive
			if (!scatter.PositiveRadiance()) return Float3.zero;
			var query = interaction.SpawnOcclude(incident, travel);
			if (arena.Scene.Occlude(ref query)) return Float3.zero;

			//Calculate final radiance
			float pdfScatter = interaction.bsdf.ProbabilityDensity(outgoing, incident);
			float weight = source.type.IsDelta() ? 1f : PowerHeuristic(pdf, pdfScatter);
			return scatter * light * (weight / pdf);
		}

		/// <summary>
		/// Importance samples <paramref name="interaction.bsdf"/> with <paramref name="source"/> and returns the combined radiance.
		/// </summary>
		static Float3 ImportanceSampleBSDF(in Interaction interaction, LightSource source, Arena arena)
		{
			//TODO: sort this mess

			Distro2 distro = arena.distribution.NextTwo();
			if (source.type.IsDelta()) return Float3.zero;

			Float3 scatter = interaction.bsdf.Sample(interaction.outgoing, distro, out Float3 incident, out float pdf, out FunctionType sampledType);

			scatter *= interaction.NormalDot(incident);

			if (!scatter.PositiveRadiance() || !FastMath.Positive(pdf)) return Float3.zero;

			float weight = 1f;

			if (!sampledType.Any(FunctionType.specular))
			{
				float pdfLight = source.ProbabilityDensity(interaction, incident);
				if (!FastMath.Positive(pdfLight)) return Float3.zero;
				weight = PowerHeuristic(pdf, pdfLight);
			}

			TraceQuery query = interaction.SpawnTrace(incident);

			Float3 light = Float3.zero;

			if (arena.Scene.Trace(ref query))
			{
				//TODO: evaluate light at intersection if area light is our source
			}
			else if (source is AmbientLight ambient)
			{
				light = ambient.Evaluate(query.ray.direction);
			}

			if (light.PositiveRadiance()) return weight / pdf * scatter * light;
			return Float3.zero;
		}

		/// <summary>
		/// Power heuristic with a constant power of two used for multiple importance sampling.
		/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float PowerHeuristic(float pdf0, float pdf1) => pdf0 * pdf0 / (pdf0 * pdf0 + pdf1 * pdf1);
	}
}