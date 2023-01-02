using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record NormalDepthEvaluator : Evaluator
{
	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer, string label) => CreateOrClearLayer<NormalDepth128>(buffer, label);

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);
		float distance = 0f;

		allocator.Restart();

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);
			contact.shade.material.Scatter(ref contact, allocator);
			distance += query.distance;

			if (contact.bsdf == null)
			{
				query = query.SpawnTrace();
				continue;
			}

			return new NormalDepth128(contact.shade.Normal, distance).ToFloat4();
		}

		//Use negative direction and scene diameter for escaped rays
		return new NormalDepth128(-ray.direction, scene.accelerator.SphereBound.radius * 2f).ToFloat4();
	}
}