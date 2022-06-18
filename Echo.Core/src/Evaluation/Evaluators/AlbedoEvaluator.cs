using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
<<<<<<< HEAD
using Echo.Core.Textures.Evaluation;
using System.Collections.Generic;
=======
using Echo.Core.Evaluation.Materials;
using Echo.Core.Textures.Evaluation;
>>>>>>> origin/main

namespace Echo.Core.Evaluation.Evaluators;

public record AlbedoEvaluator : Evaluator
{
    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "albedo");

    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        var query = new TraceQuery(ray);

        List<RGBA128> colors = new List<RGBA128>();

        //Trace for intersection
        while (scene.Trace(ref query))
        {
            Touch touch = scene.Interact(query);

            allocator.Restart();

            Material material = touch.shade.material;
            material.Scatter(ref touch, allocator);

            if (touch.bsdf == null) query = query.SpawnTrace();
<<<<<<< HEAD
            else
            {
                RGBA128 color = touch.shade.material.SampleAlbedo(touch);
                colors.Add(color);
                if (color.Alpha == 1f) return MixColors(colors);
                else query = query.SpawnTrace();
            }

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
=======
            else return (RGB128)material.SampleAlbedo(touch);
        }

        //Sample ambient
        return scene.lights.EvaluateAmbient(query.ray.direction);
>>>>>>> origin/main
    }
}