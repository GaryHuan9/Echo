using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);

		allocator.Restart();

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);

			Material material = contact.shade.material;
			material.Scatter(ref contact, allocator);

			if (contact.bsdf == null) query = query.SpawnTrace();
			else return (RGB128)material.SampleAlbedo(contact);
		}

		//Sample infinite lights
		return scene.EvaluateInfinite(query.ray.direction);
	}
}