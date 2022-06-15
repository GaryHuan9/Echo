using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Distributions.Continuous;
using CodeHelpers.Packed;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Evaluation.Scattering;


namespace Echo.Core.Evaluation.Evaluators;

public record BruteForcedEvaluator : Evaluator
{

    public int bounceLimit = 16;

    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        return Evaluate(scene, ray, distribution, allocator, bounceLimit);
    }

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

    private Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, int depth)
    {
        if (depth <= 0) return RGB128.Black;

        var query = new TraceQuery(ray);

        allocator.Restart();

        while (scene.Trace(ref query))
        {
            Touch touch = scene.Interact(query);

            var material = touch.shade.material;
            material.Scatter(ref touch, allocator);
            if (touch.bsdf != null)
            {
                Float3 incidentWorld = Float3.Zero;
                BxDF bxdf = null;
                touch.bsdf.Sample(touch.outgoing, distribution.Next2D(), out incidentWorld, out bxdf);
                return (material.SampleAlbedo(touch) + Evaluate(scene, query.SpawnTrace(incidentWorld).ray, distribution, allocator, depth - 1)) * 0.5f;
            }

            query = query.SpawnTrace();
        }

        return scene.lights.EvaluateAmbient(query.ray.direction);

    }
}