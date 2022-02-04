using System.Text;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.UserInterface.Core.Areas;

namespace EchoRenderer.UserInterface.Interface;

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
	double interval;
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
			long traceCount = engine.CurrentProfile.Scene.TraceCount;
			double rate = traceCount / engine.Elapsed.TotalSeconds;

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

		if (interval >= UpdateInterval)
		{
			lastAverage = (float)(frameCount / interval);

			frameCount = 0;
			interval = 0d;
		}

		++frameCount;

		interval += Root.application.DeltaTime;
		builder.Append($"FPS: {lastAverage:F2}");

		AppendGap();
	}

	void AppendGap() => builder.Append(" \t ");
}