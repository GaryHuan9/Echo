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

		if (!scene.Trace(ref query)) return scene.lights.EvaluateAmbient(query.ray.direction);

		Float3 energy = Float3.one;
		Float3 result = Float3.zero;

		Touch touch = scene.Interact(query);

		ref readonly Material material = ref touch.shade.material;

		//TODO: Add emissive first hit

		for (int bounce = 0; bounce < profile.BounceLimit; bounce++)
		{
			//Calculate new material bsdf
			using var _0 = arena.allocator.Begin();
			material.Scatter(ref touch, arena);

			//Work around the generated bsdf
			Sample2D scatterSample = arena.Distribution.Next2D();
			Sample2D radiantSample = arena.Distribution.Next2D();

			bool specular = touch.bsdf.Count(~FunctionType.specular) == 0;

			Float3 scatter = touch.bsdf.Sample(touch.outgoing, scatterSample, out Float3 incident, out float scatterPdf, out _);

			if (specular) { }
			else
			{
				//Select light from scene for multiple importance sampling
				ILight light = scene.PickLight(arena, out float lightPdf);
				Float3 radiant = ImportanceSampleRadiant(light, touch, arena.Distribution.Next2D(), scene, out bool mis);

				result += 1f / lightPdf * energy * radiant;
			}

			if (!scatter.PositiveRadiance() | !FastMath.Positive(scatterPdf)) break;

			float dot = touch.NormalDot(incident);
			energy *= dot / scatterPdf * scatter;

			//TODO: Path termination with Russian Roulette
			if (!energy.PositiveRadiance()) break;

			query = query.SpawnTrace(incident);

			if (scene.Trace(ref query)) { }

			//Add emission if available
			if (material.IsEmissive)
			{
				result += energy * material.Emission;
			}
		}

		return result;
	}

	/// <summary>
	/// Importance samples <paramref name="light"/> at <paramref name="touch"/> and returns the sampled radiant.
	/// If multiple importance sampling is used, <paramref name="mis"/> will be assigned to true, otherwise false.
	/// </summary>
	static Float3 ImportanceSampleRadiant(ILight light, in Touch touch, Sample2D sample, PreparedScene scene, out bool mis)
	{
		//Importance sample light
		mis = light is IAreaLight;

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
		float weight = 1f / radiantPdf;

		if (mis)
		{
			float scatterPdf = touch.bsdf.ProbabilityDensity(outgoing, incident);
			weight *= PowerHeuristic(radiantPdf, scatterPdf);
		}

		return weight * scatter * radiant;
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