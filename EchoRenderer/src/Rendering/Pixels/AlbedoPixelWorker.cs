using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Scenic.Preparation;

namespace EchoRenderer.Rendering.Pixels;

public class AlbedoPixelWorker : PixelWorker
{
	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		PreparedScene scene = profile.Scene;
		IRandom random = arena.Random;

		TraceQuery query = scene.camera.GetRay(uv, random);

		//Trace for intersection
		while (scene.Trace(ref query))
		{
			Interaction interaction = scene.Interact(query);

			Float3 albedo = Utilities.ToFloat3(interaction.shade.material.Albedo[interaction.shade.Texcoord]);
			/*if (!HitPassThrough(query, albedo, interaction.outgoing))*/ return albedo; //Return intersected albedo color

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