using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

[EchoSourceUsable]
public sealed record NormalDepthEvaluator : Evaluator
{
	/// <summary>
	/// Whether a path is allowed to diverge once before begin terminated.
	/// </summary>
	/// <remarks>Enable this for results to pass through on fully specular surfaces. Note
	/// that this does not work great with normals so it is disabled by default.</remarks>
	[EchoSourceUsable]
	public bool DivergeOnce { get; set; } = false;

	public override IEvaluationLayer CreateOrClearLayer(RenderTexture texture, string label) => CreateOrClearLayer<NormalDepth128>(texture, label);

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);
		bool straight = true;
		float depth = 0f;

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);
			Material material = contact.shade.material;

			if (!straight) return Exit();
			depth += query.distance;

			allocator.Restart();
			material.Scatter(ref contact, allocator);

			BSDF bsdf = contact.bsdf;
			Ensure.IsNotNull(bsdf);

			if (bsdf.Count(FunctionType.All) != bsdf.Count(FunctionType.Specular)) return Exit(); //Not fully specular
			var sample = contact.bsdf.Sample(contact.outgoing, distribution.Next2D(), out Float3 incident, out _);
			if (sample.NotPossible) return Exit();

			if (incident != ray.direction) straight = false;
			if (!DivergeOnce && !straight) return Exit();

			query = query.SpawnTrace(incident);

			Float4 Exit() => new NormalDepth128(contact.shade.Normal, depth).ToFloat4();
		}

		//Use negative direction and scene diameter for escaped rays
		if (straight) depth = scene.accelerator.SphereBound.radius * 2f;
		return new NormalDepth128(-ray.direction, depth).ToFloat4();
	}
}