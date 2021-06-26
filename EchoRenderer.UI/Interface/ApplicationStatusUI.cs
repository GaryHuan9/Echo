using System.Text;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
{
	public class ApplicationStatusUI : AreaUI
	{
		public ApplicationStatusUI()
		{
			PanelColor = Theme.BackgroundColor;
			Add(label);
		}

		readonly StringBuilder builder = new StringBuilder();
		readonly LabelUI label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}, Align = LabelUI.Alignment.left};

		int frameCount;
		double deltaTime;
		float lastAverage;

		public override void Update()
		{
			base.Update();
			builder.Clear();

			UpdateFPS();

			SceneViewUI sceneView = Root.Find<SceneViewUI>();
			ProgressiveRenderEngine engine = sceneView?.engine;

			if (engine == null)
			{
				builder.Append("Missing Engine");
				AppendGap();
			}
			else if (engine.CurrentState == ProgressiveRenderEngine.State.rendering)
			{
				long intersection = engine.CurrentProfile.Scene.Intersections;
				double rate = intersection / engine.Elapsed.TotalSeconds;

				builder.Append($"Rate: {rate:F2}");
				AppendGap();

				builder.Append($"Epoch: {engine.Epoch:N0}");
				AppendGap();
			}
			else
			{
				builder.Append("Engine Awaiting");
				AppendGap();
			}

			label.Text = builder.ToString();
		}

		void UpdateFPS()
		{
			const float UpdateInterval = 1f;

			if (deltaTime >= UpdateInterval)
			{
				lastAverage = (float)(frameCount / deltaTime);

				frameCount = 0;
				deltaTime = 0d;
			}

			++frameCount;

			deltaTime += Root.application.DeltaTime;
			builder.Append($"FPS: {lastAverage:F2}");

			AppendGap();
		}

		void AppendGap() => builder.Append(" \t ");
	}
}