using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record BruteForcedEvaluator : Evaluator
{
	/// <summary>
	/// The maximum number of bounces a path can have before it is immediately terminated unconditionally.
	/// If such occurrence appears, the sample becomes biased and this property should be increased.
	/// </summary>
	public int BounceLimit { get; init; } = 128;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluationStatistics statistics)
	{
		int depth = BounceLimit;
		var query = new TraceQuery(ray);

		return Evaluate(scene, ref query, distribution, allocator, ref depth);
	}

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "force");

	static Float4 Evaluate(PreparedScene scene, ref TraceQuery query, ContinuousDistribution distribution, Allocator allocator, ref int depth)
	{
		if (--depth <= 0) return RGB128.Black;

		allocator.Restart();

		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);
			Material material = contact.shade.material;
			material.Scatter(ref contact, allocator);

			if (contact.bsdf != null)
			{
				var emission = RGB128.Black;

				if (material is Emissive emissive) emission += emissive.Emit(contact.point, contact.outgoing);

				var scatterSample = distribution.Next2D();
				Probable<RGB128> sample = contact.bsdf.Sample
				(
					contact.outgoing, scatterSample,
					out Float3 incident, out _
				);

				if (sample.NotPossible | sample.content.IsZero) return emission;

				RGB128 scatter = sample.content / sample.pdf;
				scatter *= contact.NormalDot(incident);
				query = query.SpawnTrace(incident);

				return scatter * Evaluate(scene, ref query, distribution, allocator, ref depth) + emission;
			}

			query = query.SpawnTrace();
		}

		return scene.EvaluateInfinite(query.ray.direction);
	}

}