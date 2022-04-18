using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Evaluators;

public class AlbedoEvaluator : Evaluator
{
	public override RGB128 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		var scene = profile.Scene;
		var query = new TraceQuery(ray);

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Touch touch = scene.Interact(query);

			var albedo = (RGB128)touch.shade.material.SampleAlbedo(touch);
			/*if (!HitPassThrough(query, albedo, touch.outgoing))*/
			return albedo; //Return intersected albedo color

			query = query.SpawnTrace(query.ray.direction);
		}

		//Sample ambient
		return scene.lights.EvaluateAmbient(query.ray.direction);
	}
}