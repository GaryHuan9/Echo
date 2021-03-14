using System.Threading;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Rendering.Pixels
{
	public class BVHQualityWorker : PixelWorker
	{
		public BVHQualityWorker()
		{
			costGradient = new Gradient();

			const float Level = 32f;

			costGradient.Add(Level * 0f, Utilities.ToColor("#000000"));
			costGradient.Add(Level * 1f, Utilities.ToColor("#FF00FF"));
			costGradient.Add(Level * 2f, Utilities.ToColor("#0000FF"));
			costGradient.Add(Level * 3f, Utilities.ToColor("#00FFFF"));
			costGradient.Add(Level * 4f, Utilities.ToColor("#00FF00"));
			costGradient.Add(Level * 5f, Utilities.ToColor("#FFFF00"));
			costGradient.Add(Level * 6f, Utilities.ToColor("#FF0000"));
			costGradient.Add(Level * 7f, Utilities.ToColor("#FFFFFF"));
		}

		readonly Gradient costGradient;

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

			Interlocked.Add(ref totalCost, cost);
			Interlocked.Increment(ref totalSample);

			return (Float3)costGradient[cost];
		}

		public string GetQualityText()
		{
			long cost = Interlocked.Read(ref totalCost);
			long sample = Interlocked.Read(ref totalSample);

			return $"Sampled {sample:N0} samples with {cost:N0} AABB intersections and an average of {(double)cost / sample:F2} intersection per sample";
		}
	}
}