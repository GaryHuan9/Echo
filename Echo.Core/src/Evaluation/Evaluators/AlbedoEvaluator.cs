using System.Runtime.CompilerServices;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        var query = new TraceQuery(ray);

        //Trace for intersection
        while (scene.Trace(ref query))
        {
            Touch touch = scene.Interact(query);

            allocator.Restart();

            touch.shade.material.Scatter(ref touch, allocator);
            if (touch.bsdf != null) return (RGB128)touch.shade.material.SampleAlbedo(touch);

            query = query.SpawnTrace();
        }

        //Sample ambient
        return scene.lights.EvaluateAmbient(query.ray.direction);

    }

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "albedo");
}