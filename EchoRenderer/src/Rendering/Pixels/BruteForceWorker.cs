﻿using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Pixels
{
	public class BruteForceWorker : PixelWorker
	{
		public override Sample Render(Float2 uv, Arena arena)
		{
			Float3 energy = Float3.one;
			Float3 radiance = Float3.zero;

			TraceQuery query = arena.Scene.camera.GetRay(uv, arena.Random);

			for (int bounce = 0; bounce < arena.profile.BounceLimit; bounce++)
			{
				if (!arena.Scene.Trace(ref query)) break;

				Interaction interaction = arena.Scene.Interact(query, out Material material);

				material.Scatter(ref interaction, arena);

				if (interaction.bsdf == null)
				{
					query = query.SpawnTrace();
					arena.allocator.Release();
					continue;
				}

				Float3 scatter = interaction.bsdf.Sample(interaction.outgoing, arena.distribution.NextTwo(), out Float3 incident, out float pdf, out FunctionType sampledType);

				// radiance += energy * emission;

				if (!ShortMath.PositiveRadiance(scatter)) energy = Float3.zero;
				else energy *= interaction.NormalDot(incident) / pdf * scatter;

				if (!ShortMath.PositiveRadiance(energy)) break;
				query = query.SpawnTrace(incident);

				arena.allocator.Release();
			}

			if (ShortMath.PositiveRadiance(energy))
			{
				foreach (AmbientLight ambient in arena.Scene.AmbientSources) radiance += energy * ambient.Evaluate(query.ray.direction);
			}

			return radiance;
		}
	}
}