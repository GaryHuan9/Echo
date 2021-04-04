using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class BVHQualityVisualizer : PostProcessingWorker
	{
		public BVHQualityVisualizer(PostProcessingEngine engine) : base(engine)
		{
			Float4[] colors =
			{
				Utilities.ToColor("#000000"),
				Utilities.ToColor("#0000FF"),
				Utilities.ToColor("#00FFFF"),
				Utilities.ToColor("#00FF00"),
				Utilities.ToColor("#FFFF00"),
				Utilities.ToColor("#FF0000"),
				Utilities.ToColor("#FFFFFF")
			};

			costGradient = new Gradient();

			for (int i = 0; i < colors.Length; i++)
			{
				float percent = i / (colors.Length - 1f);
				costGradient.Add(percent, colors[i]);
			}
		}

		readonly Gradient costGradient;

		float maxCost;
		float totalCost;
		float totalSample;

		public override void Dispatch()
		{
			RunPass(GatherPass);
			RunPass(MainPass);

			float max = InterlockedHelper.Read(ref maxCost);
			float cost = InterlockedHelper.Read(ref totalCost);
			float sample = InterlockedHelper.Read(ref totalSample);

			Program.commandsController.Log($"Intersected {cost:N0} AABBs with an average of {cost / sample:F2} intersection per sample and a max intersection of {max:N0}.");
		}

		void GatherPass(Int2 position)
		{
			Float3 source = renderBuffer[position].XYZ;

			InterlockedHelper.Max(ref maxCost, source.x);
			InterlockedHelper.Max(ref totalCost, source.y);
			InterlockedHelper.Max(ref totalSample, source.z);
		}

		void MainPass(Int2 position)
		{
			float percent = renderBuffer[position].x / maxCost;
			renderBuffer[position] = costGradient[percent];
		}
	}
}