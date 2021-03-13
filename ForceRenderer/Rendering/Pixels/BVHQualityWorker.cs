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

			costGradient.AddAnchor(Level * 0f, Utilities.ToColor("#000000"));
			costGradient.AddAnchor(Level * 1f, Utilities.ToColor("#FF00FF"));
			costGradient.AddAnchor(Level * 2f, Utilities.ToColor("#0000FF"));
			costGradient.AddAnchor(Level * 3f, Utilities.ToColor("#00FFFF"));
			costGradient.AddAnchor(Level * 4f, Utilities.ToColor("#00FF00"));
			costGradient.AddAnchor(Level * 5f, Utilities.ToColor("#FFFF00"));
			costGradient.AddAnchor(Level * 6f, Utilities.ToColor("#FF0000"));
			costGradient.AddAnchor(Level * 7f, Utilities.ToColor("#FFFFFF"));
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

			return (Float3)costGradient.Sample(cost);
		}

		public string GetQualityText()
		{
			long cost = Interlocked.Read(ref totalCost);
			long sample = Interlocked.Read(ref totalSample);

			return $"Sampled {sample:N0} samples with {cost:N0} AABB intersections and an average of {(double)cost / sample:F2} intersection per sample";
		}
	}
}