using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record NormalEvaluator : Evaluator
{
	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer, string label) => CreateOrClearLayer<Normal96>(buffer, label);

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);

		allocator.Restart();

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);
			contact.shade.material.Scatter(ref contact, allocator);

			if (contact.bsdf == null) query = query.SpawnTrace();
			else return ((Normal96)contact.shade.Normal).ToFloat4();
		}

		//Return negative direction for escaped rays
		return -(Float4)ray.direction;
	}
}