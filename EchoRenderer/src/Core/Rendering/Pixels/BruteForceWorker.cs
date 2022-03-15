using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Scenic.Lights;

namespace EchoRenderer.Core.Rendering.Pixels;

public class BruteForceWorker : PixelWorker
{
	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		Float3 energy = Float3.one;
		Float3 radiance = Float3.zero;

		TraceQuery query = profile.Scene.camera.GetRay(uv, arena.Distribution.Prng);

		for (int bounce = 0; bounce < profile.BounceLimit; bounce++)
		{
			if (!profile.Scene.Trace(ref query)) break;
			using var _ = arena.allocator.Begin();

			Interaction interaction = profile.Scene.Interact(query);
			interaction.shade.material.Scatter(ref interaction, arena);

			if (interaction.bsdf == null)
			{
				query = query.SpawnTrace();
				continue;
			}

			Float3 scatter = interaction.bsdf.Sample(interaction.outgoing, arena.Distribution.Next2D(), out Float3 incident, out float pdf, out BxDF function);

			radiance += energy * interaction.shade.material.Emission;

			if (!scatter.PositiveRadiance()) energy = Float3.zero;
			else energy *= interaction.NormalDot(incident) / pdf * scatter;

			if (!energy.PositiveRadiance()) break;
			query = query.SpawnTrace(incident);
		}

		if (energy.PositiveRadiance())
		{
			foreach (AmbientLight ambient in profile.Scene.lights.Ambient) radiance += energy * ambient.Evaluate(query.ray.direction);
		}

		return radiance;
	}
}