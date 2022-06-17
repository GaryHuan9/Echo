using System.Reflection.PortableExecutable;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Common.Mathematics;
using Echo.Core.Scenic.Lights;
using System;
using Echo.Core.Evaluation.Distributions;

namespace Echo.Core.Evaluation.Evaluators;

public record BruteForcedEvaluator : Evaluator
{

	public int bounceLimit = 32;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
	{
		Span<Sample1D> lightSamples = stackalloc Sample1D[scene.info.depth + 1];
		return Evaluate(scene, ray, distribution, allocator, bounceLimit, new TraceQuery(ray), lightSamples);
	}

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

	private Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, int depth, TraceQuery prevQuery, Span<Sample1D> lightSamples)
	{
		if (depth <= 0) return RGB128.Black;

		foreach (ref var sample in lightSamples) sample = distribution.Next1D();

		allocator.Restart();

		while (scene.Trace(ref prevQuery))
		{
			Touch touch = scene.Interact(prevQuery);

			var material = touch.shade.material;
			material.Scatter(ref touch, allocator);
			if (touch.bsdf != null)
			{
				Float3 incidentWorld = Float3.Zero;
				Probable<RGB128> sample = touch.bsdf.Sample(touch.outgoing, distribution.Next2D(), out incidentWorld, out _);
				if (sample.content.IsZero || sample.pdf == 0f) return Float4.Zero;
				RGB128 color = sample.content / sample.pdf;

				(ILight light, float lightPdf) = scene.PickLight(lightSamples, allocator);
				float weight = 1f / lightPdf;

				color += weight * ImportanceSampleRadiant(light, touch, distribution.Next2D(), scene, out _);

				TraceQuery newTraceQuery = prevQuery.SpawnTrace(incidentWorld);
				return (color * Evaluate(scene, newTraceQuery.ray, distribution, allocator, depth - 1, newTraceQuery, lightSamples)) * touch.NormalDot(incidentWorld);
			}

			prevQuery = prevQuery.SpawnTrace();
		}

		return scene.lights.EvaluateAmbient(prevQuery.ray.direction);

	}

	static RGB128 ImportanceSampleRadiant(ILight light, in Touch touch, Sample2D sample, PreparedScene scene, out bool mis)
	{
		//Importance sample light
		mis = light is IAreaLight;

		(RGB128 radiant, float radiantPdf) = light.Sample(touch.point, sample, out Float3 incident, out float travel);

		if (!FastMath.Positive(radiantPdf) | radiant.IsZero) return RGB128.Black;

		//Evaluate bsdf at the direction sampled for our light
		ref readonly Float3 outgoing = ref touch.outgoing;
		RGB128 scatter = touch.bsdf.Evaluate(outgoing, incident);
		scatter *= touch.NormalDot(incident);

		//Conditionally terminate if radiant cannot be positive
		if (scatter.IsZero) return RGB128.Black;
		var query = touch.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return RGB128.Black;

		//Calculate final radiant
		radiant /= radiantPdf;
		if (!mis) return scatter * radiant;

		float pdf = touch.bsdf.ProbabilityDensity(outgoing, incident);
		return PowerHeuristic(radiantPdf, pdf) * scatter * radiant;
	}

	/// <summary>
	/// Power heuristic with a constant power of two used for multiple importance sampling.
	/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
	/// </summary>
	static float PowerHeuristic(float pdf0, float pdf1) => pdf0 * pdf0 / (pdf0 * pdf0 + pdf1 * pdf1);
}