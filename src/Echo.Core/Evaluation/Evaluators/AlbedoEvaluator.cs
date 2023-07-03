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

namespace Echo.Core.Evaluation.Evaluators;

[EchoSourceUsable]
public sealed record AlbedoEvaluator : Evaluator
{
	/// <summary>
	/// Whether a path is allowed to diverge once before begin terminated.
	/// </summary>
	/// <remarks>This is for better results on fully specular surfaces.</remarks>
	[EchoSourceUsable]
	public bool DivergeOnce { get; set; } = true;

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics)
	{
		var query = new TraceQuery(ray);
		bool straight = true;

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Contact contact = scene.Interact(query);
			Material material = contact.shade.material;

			if (!straight) return Exit();

			allocator.Restart();
			material.Scatter(ref contact, allocator);

			BSDF bsdf = contact.bsdf;
			Ensure.IsNotNull(bsdf);

			if (bsdf.Count != bsdf.CountSpecular) return Exit(); //Not fully specular
			var sample = contact.bsdf.Sample(contact.outgoing, distribution.Next2D(), out Float3 incident, out _);
			if (sample.NotPossible) return Exit();

			if (incident != ray.direction) straight = false;
			if (!DivergeOnce && !straight) return Exit();

			query = query.SpawnTrace(incident);

			Float4 Exit() => (RGB128)material.SampleAlbedo(contact);
		}

		//Sample infinite lights
		return scene.EvaluateInfinite(query.ray.direction);
	}
}