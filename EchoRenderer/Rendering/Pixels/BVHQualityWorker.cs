using System.Threading;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Pixels
{
	public class BVHQualityWorker : PixelWorker
	{
		long totalCost;
		long totalSample;

		public override MemoryArena CreateArena(int hash) => new MemoryArena(hash);

		public override void AssignProfile(RenderProfile profile)
		{
			base.AssignProfile(profile);

			Interlocked.Exchange(ref totalCost, 0);
			Interlocked.Exchange(ref totalSample, 0);
		}

		public override Sample Render(Float2 screenUV, MemoryArena arena)
		{
			PressedScene scene = Profile.Scene;
			Ray ray = scene.camera.GetRay(screenUV);

			int cost = scene.GetIntersectionCost(ray);

			long currentCost = Interlocked.Add(ref totalCost, cost);
			long currentSample = Interlocked.Increment(ref totalSample);

			return new Float3(cost, currentCost, currentSample);
		}
	}
}