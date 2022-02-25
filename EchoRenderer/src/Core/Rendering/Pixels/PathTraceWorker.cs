using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Scenic.Lights;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Pixels;

public class PathTraceWorker : PixelWorker
{
	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		PreparedScene scene = profile.Scene;

		Float3 energy = Float3.one;
		Float3 radiance = Float3.zero;

		bool specularBounce = false;

		TraceQuery query = scene.camera.GetRay(uv);

		for (int bounce = 0;; bounce++)
		{
			bool intersected = scene.Trace(ref query);

			if (bounce == 0 || specularBounce)
			{
				//TODO: Take care of emitted light at this path vertex or from the environment

				if (intersected) { }
				else
				{
					foreach (AmbientLight ambient in scene.AmbientLights)
					{
						radiance += energy * ambient.Evaluate(query.ray.direction);
					}
				}
			}

			if (!intersected || bounce > profile.BounceLimit) break;

			using var _ = arena.allocator.Begin();

			Interaction interaction = scene.Interact(query);
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
				interaction.outgoing, arena.distribution.Next2D(),
				out Float3 incident, out float pdf, out FunctionType sampledType
			);

			//TODO: this is a mess

			if (!scatter.PositiveRadiance() || !FastMath.Positive(pdf)) break;

			ILight source = scene.PickLight(arena, out float pdfLight);

			Float3 light = ImportanceSampleLight(interaction, source, scene, arena);
			light += ImportanceSampleBSDF(interaction, source, scene, arena);

			radiance += 1f / pdfLight * energy * light;

			energy *= interaction.NormalDot(incident) / pdf * scatter;
			specularBounce = sampledType.Any(FunctionType.specular);

			if (!energy.PositiveRadiance()) break;

			query = query.SpawnTrace(incident);

			//TODO: Path termination with Russian Roulette
		}

		return radiance;
	}

	// /// <summary>
	// /// Returns a <see cref="LightSource"/> in <see cref="Arena.Scene"/> and outputs its <paramref name="pdf"/>.
	// /// </summary>
	// static LightSource FindLight(in Interaction interaction, Arena arena, out float pdf)
	// {
	// 	//Handle degenerate cases
	// 	var sources = arena.Scene.lightSources;
	// 	int length = sources.Length;
	//
	// 	if (length == 0)
	// 	{
	// 		pdf = 0f;
	// 		return null;
	// 	}
	//
	// 	//Finds one light to sample
	// 	pdf = 1f / length;
	// 	return sources[arena.distribution.NextOne().Range(length)];
	// }

	/// <summary>
	/// Importance samples <paramref name="light"/> at <paramref name="interaction"/> and returns the combined radiance.
	/// </summary>
	static Float3 ImportanceSampleLight(in Interaction interaction, ILight light, PreparedScene scene, Arena arena)
	{
		//Sample light source
		Float3 emission = light.Sample
		(
			interaction.point, arena.distribution.Next2D(),
			out Float3 incident, out float pdf, out float travel
		);

		if (!FastMath.Positive(pdf) || !emission.PositiveRadiance()) return Float3.zero;

		//Evaluate bsdf at light source's directions
		ref readonly Float3 outgoing = ref interaction.outgoing;
		Float3 scatter = interaction.bsdf.Evaluate(outgoing, incident);
		scatter *= interaction.NormalDot(incident);

		//Conditionally terminate if radiance is non-positive
		if (!scatter.PositiveRadiance()) return Float3.zero;
		var query = interaction.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return Float3.zero;

		//Calculate final radiance
		float pdfScatter = interaction.bsdf.ProbabilityDensity(outgoing, incident);
		float weight = light is IAreaLight ? PowerHeuristic(pdf, pdfScatter) : 1f;
		return scatter * emission * (weight / pdf);
	}

	/// <summary>
	/// Importance samples <paramref name="interaction.bsdf"/> with <paramref name="light"/> and returns the combined radiance.
	/// </summary>
	static Float3 ImportanceSampleBSDF(in Interaction interaction, ILight light, PreparedScene scene, Arena arena)
	{
		//TODO: sort this mess

		Sample2D sample = arena.distribution.Next2D();
		if (light is not IAreaLight areaLight) return Float3.zero;

		Float3 scatter = interaction.bsdf.Sample(interaction.outgoing, sample, out Float3 incident, out float pdf, out FunctionType sampledType);

		scatter *= interaction.NormalDot(incident);

		if (!scatter.PositiveRadiance() || !FastMath.Positive(pdf)) return Float3.zero;

		float weight = 1f;

		if (!sampledType.Any(FunctionType.specular))
		{
			float pdfLight = areaLight.ProbabilityDensity(interaction.point, incident);
			if (!FastMath.Positive(pdfLight)) return Float3.zero;
			weight = PowerHeuristic(pdf, pdfLight);
		}

		TraceQuery query = interaction.SpawnTrace(incident);

		Float3 emission = Float3.zero;

		if (scene.Trace(ref query))
		{
			//TODO: evaluate light at intersection if area light is our source
		}
		else if (areaLight is AmbientLight ambient)
		{
			emission = ambient.Evaluate(incident);
		}

		if (!emission.PositiveRadiance()) return Float3.zero;
		return weight / pdf * scatter * emission;
	}

	/// <summary>
	/// Power heuristic with a constant power of two used for multiple importance sampling.
	/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static float PowerHeuristic(float pdf0, float pdf1) => pdf0 * pdf0 / (pdf0 * pdf0 + pdf1 * pdf1);
}