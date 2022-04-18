using System.Threading;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Evaluators;

public class AggregatorQualityEvaluator : Evaluator
{
	long totalCost;
	long totalSample;

	public override RGB128 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		var scene = profile.Scene;
		int cost = scene.TraceCost(ray);

		long currentCost = Interlocked.Add(ref totalCost, cost);
		long currentSample = Interlocked.Increment(ref totalSample);

		return new RGB128(cost, currentCost, currentSample);
	}

	protected override ContinuousDistribution CreateDistribution(RenderProfile profile)
	{
		Interlocked.Exchange(ref totalCost, 0);
		Interlocked.Exchange(ref totalSample, 0);

		return base.CreateDistribution(profile);
	}
}