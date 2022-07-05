using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "albedo");

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluationStatistics statistics)
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

		//Sample ambient
		return scene.EvaluateInfinite(query.ray.direction);
	}
}