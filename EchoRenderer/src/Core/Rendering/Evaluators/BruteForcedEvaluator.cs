using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Scenic.Lights;

namespace EchoRenderer.Core.Rendering.Evaluators;

public class BruteForcedEvaluator : Evaluator
{
	public override Float3 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		Float3 energy = Float3.one;
		Float3 radiance = Float3.zero;

		TraceQuery query = ray;

		for (int bounce = 0; bounce < profile.BounceLimit; bounce++)
		{
			if (!profile.Scene.Trace(ref query)) break;
			using var _ = arena.allocator.Begin();

			Touch touch = profile.Scene.Interact(query);
			touch.shade.material.Scatter(ref touch, arena);

			if (touch.bsdf == null)
			{
				query = query.SpawnTrace();
				continue;
			}

			Float3 scatter = touch.bsdf.Sample(touch.outgoing, arena.Distribution.Next2D(), out Float3 incident, out float pdf, out BxDF function);

			radiance += energy * touch.shade.material.Emission;

			if (!scatter.PositiveRadiance()) energy = Float3.zero;
			else energy *= touch.NormalDot(incident) / pdf * scatter;

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