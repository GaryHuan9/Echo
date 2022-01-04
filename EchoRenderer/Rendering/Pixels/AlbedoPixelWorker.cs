using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			PreparedScene scene = arena.profile.Scene;
			IRandom random = arena.Random;

			TraceQuery query = scene.camera.GetRay(uv, random);

			//Trace for intersection
			while (scene.Trace(ref query))
			{
				Interaction interaction = scene.Interact(query, out Material material);

				Float3 albedo = Utilities.ToFloat3(material.Albedo[interaction.texcoord]);
				if (!HitPassThrough(query, albedo, interaction.outgoingWorld)) return albedo; //Return intersected albedo color

				query = query.SpawnTrace(query.ray.direction);
			}

			//Sample skybox
			Vector128<float> radiance = Vector128<float>.Zero;

			foreach (DirectionalTexture skybox in scene.Skyboxes)
			{
				radiance = Sse.Add(radiance, skybox.Evaluate(query.ray.direction));
			}

			return Utilities.ToFloat3(radiance);
		}
	}
}