using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Evaluation.Scattering;


namespace Echo.Core.Evaluation.Evaluators;

public record BruteForcedEvaluator : Evaluator
{

	public int bounceLimit = 32;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
	{
		return Evaluate(scene, ray, distribution, allocator, bounceLimit, new TraceQuery(ray));
	}

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

	private Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, int depth, TraceQuery prevQuery)
	{
		if (depth <= 0) return RGB128.Black;

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
				TraceQuery newTraceQuery = prevQuery.SpawnTrace(incidentWorld);
				return (color * Evaluate(scene, newTraceQuery.ray, distribution, allocator, depth - 1, newTraceQuery)) * touch.NormalDot(incidentWorld);
			}

			prevQuery = prevQuery.SpawnTrace();
		}

		return scene.lights.EvaluateAmbient(prevQuery.ray.direction);

	}
}