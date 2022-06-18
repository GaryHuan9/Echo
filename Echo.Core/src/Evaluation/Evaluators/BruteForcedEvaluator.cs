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

// before optimization: time of all workers: ~30sec

public record BruteForcedEvaluator : Evaluator
{

	public static readonly int bounceLimit = 32;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
	{
		allocator.Restart();
		return Evaluate(scene, ray, distribution, allocator, bounceLimit, new TraceQuery(ray));
	}

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

	static Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, int depth, TraceQuery prevQuery)
	{
		if (depth <= 0) return RGB128.Black;
		//allocator.Restart();

		while (scene.Trace(ref prevQuery))
		{
			Touch touch = scene.Interact(prevQuery);

			Material material = touch.shade.material;
			material.Scatter(ref touch, allocator);
			if (touch.bsdf == null)
			{
				prevQuery = prevQuery.SpawnTrace();
				continue;
			}
			Float3 incidentWorld = Float3.Zero;
			Probable<RGB128> sample = touch.bsdf.Sample(touch.outgoing, distribution.Next2D(), out incidentWorld, out _);
			if (material is IEmissive)
			{
				return ((IEmissive)material).Emit(touch.point, touch.outgoing);
			}

			if (sample.content.IsZero || sample.pdf == 0f) return Float4.Zero;
			RGB128 color = sample.content / sample.pdf;

			TraceQuery newTraceQuery = prevQuery.SpawnTrace(incidentWorld);
			return (color * Evaluate(scene, newTraceQuery.ray, distribution, allocator, depth - 1, newTraceQuery)) * touch.NormalDot(incidentWorld);
		}

		return scene.lights.EvaluateAmbient(prevQuery.ray.direction);

	}

}