using CodeHelpers.Diagnostics;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
{
	public class ApplicationStatusUI : AreaUI
	{
		public ApplicationStatusUI()
		{
			transform.TopMargin = -Theme.LayoutHeight;
			transform.TopPercent = 1f;

			PanelColor = Theme.BackgroundColor;
			Add(label);
		}

		readonly LabelUI label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}, Align = LabelUI.Alignment.left};

		int frameCount;
		double deltaTime;
		float lastAverage;

		const float AverageInterval = 1f;

		public override void Update()
		{
			base.Update();

			if (deltaTime >= AverageInterval)
			{
				lastAverage = (float)(frameCount / deltaTime);

				frameCount = 0;
				deltaTime = 0d;
			}

			++frameCount;

			deltaTime += Root.application.DeltaTime;
			label.Text = $"FPS: {lastAverage:F2}";
		}
	}
}