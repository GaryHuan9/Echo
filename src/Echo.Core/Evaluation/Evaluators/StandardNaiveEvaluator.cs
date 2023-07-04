using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Evaluators;

/// <summary>
/// A standard naive unidirectional path traced <see cref="Evaluator"/> able to simulate
/// complex lighting effects such as global illumination and ambient occlusion.
/// </summary>
/// <remarks>This provides a simpler example of an implementation of the rendering equation. Can also be used
/// as a reference <see cref="Evaluator"/> to generate ground truth renders and test for correctness of other
/// <see cref="Evaluator"/>. Note that this <see cref="Evaluator"/> does not support delta (singularity) lights.</remarks>
[EchoSourceUsable]
public record StandardNaiveEvaluator : Evaluator
{
	/// <summary>
	/// The maximum number of bounces a path can have before it is immediately terminated unconditionally.
	/// If such occurrence appears, the sample becomes biased and this property should be increased.
	/// </summary>
	[EchoSourceUsable]
	public int BounceLimit { get; init; } = 128;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);
		return Evaluate(scene, ref query, distribution, allocator, 0);
	}

	RGB128 Evaluate(PreparedScene scene, ref TraceQuery query, ContinuousDistribution distribution, Allocator allocator, int depth)
	{
		//Early exit if path depth has been exhausted or the ray did not hit the scene
		if (depth == BounceLimit || !scene.Trace(ref query))
		{
			bool direct = depth == 0; //Escaped without hitting any geometry
			return scene.EvaluateInfinite(query.ray.direction, direct);
		}

		allocator.Restart();

		//Begin another interaction between the path and the scene
		Contact contact = scene.Interact(query);
		Material material = contact.shade.material;
		material.Scatter(ref contact, allocator);

		//Samples emission and BSDF properties at the Contact
		RGB128 emission = material is Emissive emissive ? emissive.Emit(contact.point, contact.outgoing) : RGB128.Black;
		Probable<RGB128> sample = contact.bsdf.Sample(contact.outgoing, distribution.Next2D(), out Float3 incident, out _);

		//Exit if the BSDF sample is not promising
		if (sample.NotPossible || sample.content.IsZero) return emission;

		//Accumulate results and generate new query for the next bounce
		RGB128 scatter = sample.content / sample.pdf;
		scatter *= contact.NormalDot(incident);
		query = query.SpawnTrace(incident);

		//Recursively evaluates the rendering equation
		return scatter * Evaluate(scene, ref query, distribution, allocator, depth + 1) + emission;
	}
}