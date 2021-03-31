﻿using System.Threading;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Rendering.Pixels
{
	public class BVHQualityWorker : PixelWorker
	{
		long totalCost;
		long totalSample;

		public override void AssignProfile(PressedRenderProfile profile)
		{
			base.AssignProfile(profile);

			Interlocked.Exchange(ref totalCost, 0);
			Interlocked.Exchange(ref totalSample, 0);
		}

		public override Float3 Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			int cost = scene.bvh.GetIntersectionCost(ray);

			long currentCost = Interlocked.Add(ref totalCost, cost);
			long currentSample = Interlocked.Increment(ref totalSample);

			return new Float3(cost, currentCost, currentSample);
		}
	}
}