using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Rendering.Evaluators;

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