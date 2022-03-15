using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Distributions.Continuous;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Scenic.Lights;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Pixels;

public class PathTraceWorker : PixelWorker
{
	protected override ContinuousDistribution CreateDistribution(RenderProfile profile) => new StratifiedDistribution(profile.TotalSample);

	//NOTE: although the word 'radiant' is frequently used here to denote particles of energy accumulating,
	//technically it is not correct because the size of the emitter could be either a point or an area,
	//(so for the same reason the word 'radiance' is also wrong) we just chose 'radiant' because it has the
	//same length as the word 'scatter'.

	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		PreparedScene scene = profile.Scene;
		TraceQuery query = scene.camera.GetRay(uv);

		Float3 energy = Float3.one;
		Float3 result = Float3.zero;

		float scatterPdf = 1f;
		float radiantPdf = 1f;

		for (int bounce = 0;; bounce++)
		{
			//Exits loop if we have reached our bounce limit or we hit no geometry
			if (bounce > profile.BounceLimit || !scene.Trace(ref query))
			{
				result += energy * scene.lights.EvaluateAmbient(query.ray.direction);
				break;
			}

			//Interact with the scene at our intersection
			Touch touch = scene.Interact(query);

			//Add emission if available
			ref readonly Material material = ref touch.shade.material;
			if (material.IsEmissive) result += energy * material.Emission;

			//Calculate material bsdf
			using var _0 = arena.allocator.Begin();
			material.Scatter(ref touch, arena);

			if (touch.bsdf == null)
			{
				query = query.SpawnTrace();
				--bounce;
				continue;
			}

			//Sample the calculated bsdf
			Sample2D scatterSample = arena.Distribution.Next2D();
			Sample2D radiantSample = arena.Distribution.Next2D();

			Float3 scatter = touch.bsdf.Sample(touch.outgoing, scatterSample, out Float3 incident, out scatterPdf, out BxDF function);

			if (!scatter.PositiveRadiance() || !FastMath.Positive(scatterPdf)) break;

			//Select light from scene for multiple importance sampling
			ILight light = scene.PickLight(arena, out float lightPdf);

			Float3 emission = ImportanceSampleLight(light, touch, arena.Distribution.Next2D(), scene);

			// if (light is IAreaLight area and not GeometryLight)
			// {
			// 	float weight = 1f;
			//
			// 	if (!function.type.Any(FunctionType.specular))
			// 	{
			// 		float p = area.ProbabilityDensity(touch.point, incident);
			// 		// if (!FastMath.Positive(p)) return Float3.zero;
			// 		weight = PowerHeuristic(pdf, p);
			// 	}
			//
			// 	emission += weight * area.
			// }

			float dot = touch.NormalDot(incident);
			result += 1f / lightPdf * energy * emission;
			energy *= dot / scatterPdf * scatter;

			//TODO: Path termination with Russian Roulette
			if (!energy.PositiveRadiance()) break;

			query = query.SpawnTrace(incident);
		}

		return result;
	}

	/// <summary>
	/// Importance samples <paramref name="light"/> at <paramref name="touch"/> and returns the emission/radiant.
	/// </summary>
	static Float3 ImportanceSampleLight(ILight light, in Touch touch, Sample2D sample, PreparedScene scene)
	{
		//Sample light
		Float3 radiant = light.Sample(touch.point, sample, out Float3 incident, out float radiantPdf, out float travel);

		if (!FastMath.Positive(radiantPdf) | !radiant.PositiveRadiance()) return Float3.zero;

		//Evaluate bsdf at the direction sampled for our light
		ref readonly Float3 outgoing = ref touch.outgoing;
		Float3 scatter = touch.bsdf.Evaluate(outgoing, incident);
		scatter *= touch.NormalDot(incident);

		//Conditionally terminate if radiant cannot be positive
		if (!scatter.PositiveRadiance()) return Float3.zero;
		var query = touch.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return Float3.zero;

		//Calculate final radiant
		if (light is not IAreaLight) return 1f / radiantPdf * scatter * radiant;
		float scatterPdf = touch.bsdf.ProbabilityDensity(outgoing, incident);
		return PowerHeuristic(radiantPdf, scatterPdf) / radiantPdf * scatter * radiant;
	}

	/// <summary>
	/// Importance samples <paramref name="touch.bsdf"/> with <paramref name="light"/> and returns the combined radiance.
	/// </summary>
	static Float3 ImportanceSampleBSDF(in Touch touch, ILight light, PreparedScene scene, Arena arena)
	{
		//TODO: sort this mess

		Sample2D sample = arena.Distribution.Next2D();
		if (light is not IAreaLight area) return Float3.zero;

		Float3 scatter = touch.bsdf.Sample(touch.outgoing, sample, out Float3 incident, out float pdf, out BxDF function);

		scatter *= touch.NormalDot(incident);

		if (!scatter.PositiveRadiance() || !FastMath.Positive(pdf)) return Float3.zero;

		float weight = 1f;

		if (!function.type.Any(FunctionType.specular))
		{
			float pdfLight = area.ProbabilityDensity(touch.point, incident);
			if (!FastMath.Positive(pdfLight)) return Float3.zero;
			weight = PowerHeuristic(pdf, pdfLight);
		}

		TraceQuery query = touch.SpawnTrace(incident);

		Float3 emission = Float3.zero;

		if (scene.Trace(ref query))
		{
			//Evaluate light at intersection if area light is our source

			if (area is GeometryLight geometry)
			{
				Touch other = scene.Interact(query);

				if (other.token == geometry.Token)
				{
					emission = geometry.Emission;
				}
			}
		}
		else if (area is AmbientLight ambient)
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