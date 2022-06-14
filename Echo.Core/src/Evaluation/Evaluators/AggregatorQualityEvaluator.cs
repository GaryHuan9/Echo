using System.Threading;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Evaluation.Operations;
using CodeHelpers.Packed;

namespace Echo.Core.Evaluation.Evaluators;

public record AggregatorQualityEvaluator : Evaluator
{
    long totalCost;
    long totalSample;

    public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
    {
        int cost = scene.TraceCost(ray);

        long currentCost = Interlocked.Add(ref totalCost, cost);
        long currentSample = Interlocked.Increment(ref totalSample);

        return new RGB128(cost, currentCost, currentSample);
    }

    // protected override ContinuousDistribution CreateDistribution(Echo.Core.Evaluation.Engines.RenderProfile profile)
    // {
    //     Interlocked.Exchange(ref totalCost, 0);
    //     Interlocked.Exchange(ref totalSample, 0);

    //     return base.CreateDistribution(profile);
    // }

    public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "aggregator_quality");
}