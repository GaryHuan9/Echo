using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Evaluation.Materials;
using EchoRenderer.Core.Evaluation.Scattering;
using EchoRenderer.Core.Textures.Colors;

namespace EchoRenderer.Core.Evaluation.Evaluators;

public class BruteForcedEvaluator : PathTracedEvaluator //Interesting inheritance, we will probably remove this later
{
	public override RGB128 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		var scene = profile.Scene;

		var energy = RGB128.White;
		var radiant = RGB128.Black;

		var query = new TraceQuery(ray);

		for (int bounce = 0; bounce < BounceLimit; bounce++)
		{
			if (!profile.Scene.Trace(ref query)) break;
			using var _ = arena.allocator.Begin();

			Touch touch = profile.Scene.Interact(query);
			touch.shade.material.Scatter(ref touch, arena.allocator);

			if (touch.bsdf == null)
			{
				query = query.SpawnTrace();
				continue;
			}

			(RGB128 scatter, float pdf) = touch.bsdf.Sample(touch.outgoing, arena.Distribution.Next2D(), out Float3 incident, out BxDF function);
			if (touch.shade.material is IEmissive emissive && FastMath.Positive(emissive.Power)) radiant += energy * emissive.Emit(touch.point, touch.outgoing);

			if (!FastMath.Positive(pdf) | scatter.IsZero) energy = RGB128.Black;
			else energy *= touch.NormalDot(incident) / pdf * scatter;

			if (energy.IsZero) break;
			query = query.SpawnTrace(incident);
		}

		if (!energy.IsZero) radiant += energy * scene.lights.EvaluateAmbient(query.ray.direction);

		return radiant;
	}
}