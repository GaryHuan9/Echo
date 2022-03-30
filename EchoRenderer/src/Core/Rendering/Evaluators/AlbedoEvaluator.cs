using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Rendering.Evaluators;

public class AlbedoEvaluator : Evaluator
{
	public override Float3 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		var scene = profile.Scene;
		var query = new TraceQuery(ray);

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Touch touch = scene.Interact(query);

			Float3 albedo = Utilities.ToFloat3(touch.shade.material.Albedo[touch.shade.Texcoord]);
			/*if (!HitPassThrough(query, albedo, touch.outgoing))*/
			return albedo; //Return intersected albedo color

			query = query.SpawnTrace(query.ray.direction);
		}

		//Sample skybox
		Vector128<float> radiance = Vector128<float>.Zero;

		// foreach (DirectionalTexture skybox in scene.Skyboxes)
		// {
		// 	radiance = Sse.Add(radiance, skybox.Evaluate(query.ray.direction));
		// }

		return Utilities.ToFloat3(radiance);
	}
}