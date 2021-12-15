using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			RenderProfile profile = arena.profile;
			PressedScene scene = profile.Scene;
			IRandom random = arena.Random;

			TraceQuery query = scene.camera.GetRay(uv, random);

			Float3 energy = Float3.one;
			Float3 colors = Float3.zero;

			int bounce = 0;

			//Auxiliary data
			Float3 firstAlbedo = Float3.zero;
			Float3 firstNormal = Texture.normal.Color;
			float firstZDepth = float.PositiveInfinity;

			bool missingAuxiliary = true;

			while (bounce < profile.BounceLimit && scene.Trace(ref query))
			{
				++bounce;

				Interaction interaction = scene.Interact(query, out Material material);

				// Float3 emission = material.Emit(query, random);
				// Float3 albedo = material.BidirectionalScatter(query, random, out Float3 direction);

				Float3 emission = default;
				Float3 albedo = default;

				Float3 incidentWorld = default;

				if (HitPassThrough(query, albedo, incidentWorld))
				{
					query = query.SpawnTrace();
					continue;
				}

				if (missingAuxiliary)
				{
					firstAlbedo = albedo;
					firstNormal = interaction.normal;
					firstZDepth = query.distance;

					missingAuxiliary = false;
				}

				colors += energy * emission;
				energy *= albedo;

				if (energy <= profile.RadianceEpsilon) break;
				query = query.SpawnTrace(incidentWorld);
			}

#if DEBUG
			//The bounce limit is supposed to be significantly higher than the average bounce count
			if (bounce >= profile.BounceLimit) CodeHelpers.Diagnostics.DebugHelper.Log("Bounce limit reached!");
#endif

			var cubemap = scene.cubemap;

			if (cubemap != null) colors += energy * cubemap.Sample(query.ray.direction);

			if (bounce > 0)
			{
				//Add light colors if we actually hit some geometry

				// foreach (ref readonly PressedLight light in scene.lights.AsSpan())
				// {
				// 	float weight = -light.direction.Dot(query.ray.direction);
				// 	if (weight > light.threshold) colors += energy * light.intensity * weight;
				// }
			}
			else firstAlbedo = colors; //No bounce sample do not have albedo so we just use the skybox

			return new Sample(colors.Max(Float3.zero), firstAlbedo, firstNormal, firstZDepth);
		}
	}
}