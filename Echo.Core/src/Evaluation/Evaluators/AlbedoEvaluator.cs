using System.Runtime.CompilerServices;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;
using System.Collections.Generic;

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        var query = new TraceQuery(ray);

        List<RGBA128> colors = new List<RGBA128>();

        //Trace for intersection
        while (scene.Trace(ref query))
        {
            Touch touch = scene.Interact(query);

            allocator.Restart();

            touch.shade.material.Scatter(ref touch, allocator);
            if (touch.bsdf != null) return (RGB128)touch.shade.material.SampleAlbedo(touch);

            if (touch.bsdf == null) query = query.SpawnTrace();
            else
            {
                RGBA128 color = touch.shade.material.SampleAlbedo(touch);
                colors.Add(color);
                if (color.Alpha == 1f) return MixColors(colors);
                else query = query.SpawnTrace();
            }


            //return (RGB128)material.SampleAlbedo(touch);
        }

        //Sample ambient
        RGB128 ambient = scene.lights.EvaluateAmbient(query.ray.direction);
        colors.Add((RGBA128)ambient);
        return MixColors(colors);
    }

    private RGB128 MixColors(List<RGBA128> colors)
    {
        // Due to the last color not being transparent we use it as a base
        RGB128 result = (RGB128)colors[colors.Count - 1];

        for (int i = colors.Count - 2; i >= 0; i--)
        {
            result = (result * (1f - colors[i].Alpha)) + ((RGB128)colors[i] * colors[i].Alpha);
        }

        return result;
    }

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "albedo");
}