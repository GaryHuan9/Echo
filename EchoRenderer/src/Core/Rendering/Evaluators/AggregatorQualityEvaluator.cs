using System.Threading;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Rendering.Distributions.Continuous;

namespace EchoRenderer.Core.Rendering.Evaluators;

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