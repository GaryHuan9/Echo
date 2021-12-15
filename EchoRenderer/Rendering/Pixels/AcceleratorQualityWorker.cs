using System.Threading;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Pixels
{
	public class AcceleratorQualityWorker : PixelWorker
	{
		long totalCost;
		long totalSample;

		public override Sample Render(Float2 uv, Arena arena)
		{
			PressedScene scene = arena.profile.Scene;
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
}