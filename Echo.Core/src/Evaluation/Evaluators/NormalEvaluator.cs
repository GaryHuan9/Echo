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

public record NormalEvaluator : Evaluator
{
    public override Float4 Evaluate(PreparedScene preparedScene, in Ray ray, ContinuousDistribution continuousDistribution, Allocator allocator)
    {
        var query = new TraceQuery(ray);

        allocator.Restart();

        //Trace for intersection
        while (preparedScene.Trace(ref query))
        {
            Touch touch = preparedScene.Interact(query);

            touch.shade.material.Scatter(ref touch, allocator);

            if (touch.bsdf != null) return (Float4)touch.shade.Normal;

            query = query.SpawnTrace();
        }

        return (Float4)ray.direction;
    }

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<Normal96>(buffer, "normal");
}