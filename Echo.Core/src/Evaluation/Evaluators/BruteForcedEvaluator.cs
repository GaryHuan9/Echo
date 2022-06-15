using System;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;

namespace Echo.Core.Evaluation.Evaluators;

public record BruteForcedEvaluator : PathTracedEvaluator //Interesting inheritance, we will probably remove this later
{

    public int bounceLimit = 4;

    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        var query = new TraceQuery(ray);
        RGB128 resultColor = RGB128.Black;
        int collisions = 0;

        for (int i = 0; i < bounceLimit; i++)
        {

            while (scene.Trace(ref query))
            {
                collisions++;
                Touch touch = scene.Interact(query);

                allocator.Restart();

                var material = touch.shade.material;

                material.Scatter(ref touch, allocator);

                if (touch.bsdf != null)
                {
                    float exp = MathF.Pow(0.5f, i);
                    resultColor += (RGB128)material.SampleAlbedo(touch) * new RGB128(exp, exp, exp);

                    query = touch.SpawnTrace();
                    break;
                }

                query = query.SpawnTrace();

            }

        }
        //if (collisions == 0) return scene.lights.EvaluateAmbient(query.ray.direction);

        return resultColor;
    }
}