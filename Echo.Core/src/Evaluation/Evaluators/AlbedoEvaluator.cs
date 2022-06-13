using System.Runtime.CompilerServices;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
	public override Float4 Evaluate(PreparedScene preparedScene, in Ray ray, ContinuousDistribution continuousDistribution, Allocator allocator)
	{
		var query = new TraceQuery(ray);

		//Trace for intersection
		while (preparedScene.Trace(ref query))
		{
			Touch touch = preparedScene.Interact(query);

			var albedo = (RGB128)touch.shade.material.SampleAlbedo(touch);
			if (!HitPassThrough(query, albedo, touch.outgoing))
			    return albedo; //Return intersected albedo color

			query = query.SpawnTrace();
		}

		//Sample ambient
		return preparedScene.lights.EvaluateAmbient(query.ray.direction);
	}

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGBA128>(buffer, "albedo");
}