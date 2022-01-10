using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorkerOld : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			RenderProfile profile = arena.profile;
			PreparedScene scene = profile.Scene;
			IRandom random = arena.Random;

			TraceQuery query = scene.camera.GetRay(uv, random);

			Float3 energy = Float3.one;
			Float3 colors = Float3.zero;

			int bounce = 0;

			while (bounce < profile.BounceLimit && scene.Trace(ref query))
			{
				++bounce;

				Interaction interaction = scene.Interact(query, out Material material);

				material.Scatter(ref interaction, arena);

				if (interaction.bsdf == null)
				{
					query = query.SpawnTrace();
					continue;
				}

				Float3 scatter = interaction.bsdf.Sample(interaction.outgoingWorld, arena.distribution.NextTwo(), out Float3 incidentWorld, out float pdf, out FunctionType sampledType);

				// colors += energy * emission;
				energy *= scatter / pdf;

				if (energy <= profile.RadianceEpsilon) break;
				query = query.SpawnTrace(incidentWorld);
			}

#if DEBUG
			//The bounce limit is supposed to be significantly higher than the average bounce count
			if (bounce >= profile.BounceLimit) CodeHelpers.Diagnostics.DebugHelper.Log("Bounce limit reached!");
#endif

			foreach (AmbientLight ambient in scene.AmbientSources) colors += energy * ambient.Evaluate(query.ray.direction);

			return colors.Max(Float3.zero);
		}
	}
}