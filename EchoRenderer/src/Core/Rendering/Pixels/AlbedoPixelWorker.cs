using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Pixels;

public class AlbedoPixelWorker : PixelWorker
{
	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		PreparedScene scene = profile.Scene;
		IRandom random = arena.Distribution.Prng;

		TraceQuery query = scene.camera.GetRay(uv, random);

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Touch touch = scene.Interact(query);

			Float3 albedo = Utilities.ToFloat3(touch.shade.material.Albedo[touch.shade.Texcoord]);
			/*if (!HitPassThrough(query, albedo, touch.outgoing))*/ return albedo; //Return intersected albedo color

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