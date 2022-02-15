﻿using System.Threading;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Pixels;

public class AggregatorQualityWorker : PixelWorker
{
	long totalCost;
	long totalSample;

	public override Sample Render(Float2 uv, RenderProfile profile, Arena arena)
	{
		PreparedScene scene = profile.Scene;
		Ray ray = scene.camera.GetRay(uv);

		int cost = scene.TraceCost(ray);

		long currentCost = Interlocked.Add(ref totalCost, cost);
		long currentSample = Interlocked.Increment(ref totalSample);

		return new Float3(cost, currentCost, currentSample);
	}

	protected override Distribution CreateDistribution(RenderProfile profile)
	{
		Interlocked.Exchange(ref totalCost, 0);
		Interlocked.Exchange(ref totalSample, 0);

		return new UniformDistribution(profile.TotalSample) { Jitter = profile.TotalSample > 1 };
	}
}