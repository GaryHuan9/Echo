using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
{
	public class SceneViewUI : AreaUI
	{
		public SceneViewUI()
		{
			transform.LeftPercent = 0.16f;
			transform.RightPercent = 0.28f;
			transform.UniformMargins = Theme.LargeMargin;

			engine = new ProgressiveRenderEngine();
			renderPreview = new RenderPreviewUI();

			Add(renderPreview);

			Profile = new ProgressiveRenderProfile
					  {
						  Method = new PathTraceWorker(),
						  EpochSample = 2,
						  EpochLength = 24,
						  AdaptiveSample = 35
					  };

			new Thread(LoadScene<TestMaterials>)
			{
				IsBackground = true,
				Name = "Scene Loader"
			}.Start();
		}

		public readonly ProgressiveRenderEngine engine;
		public float RedrawInterval { get; set; } = 0.3f;

		ProgressiveRenderProfile _profile;
		float _resolutionMultiplier = 0.5f;

		public ProgressiveRenderProfile Profile
		{
			get => _profile;
			set => _profile = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		public float ResolutionMultiplier
		{
			get => _resolutionMultiplier;
			set
			{
				if (_resolutionMultiplier.AlmostEquals(value)) return;

				_resolutionMultiplier = value;
				CheckResolutionChange();
			}
		}

		readonly RenderPreviewUI renderPreview;

		bool requestingRedraw;
		float lastInterval;

		public override void Update()
		{
			base.Update();

			switch (engine.CurrentState)
			{
				case ProgressiveRenderEngine.State.waiting:
				{
					try
					{
						Profile.Validate();
						engine.Begin(Profile);
					}
					catch (Exception)
					{
						// ignored
					}

					break;
				}
				case ProgressiveRenderEngine.State.rendering:
				{
					float time = (float)Root.application.TotalTime;

					if (requestingRedraw && lastInterval + RedrawInterval < time)
					{
						engine.Stop();

						requestingRedraw = false;
						lastInterval = time;
					}

					break;
				}
			}
		}

		protected override void Reorient(Float2 position, Float2 dimension)
		{
			base.Reorient(position, dimension);
			CheckResolutionChange();
		}

		public override void Dispose()
		{
			base.Dispose();

			engine.Stop();
			engine.Dispose();
		}

		public void RequestRedraw() => requestingRedraw = true;

		void CheckResolutionChange()
		{
			Int2 resolution = (Dimension * ResolutionMultiplier).Rounded;
			if (resolution == Profile.RenderBuffer?.size) return;

			var buffer = new ProgressiveRenderBuffer(resolution);

			Profile = Profile with {RenderBuffer = buffer};
			renderPreview.RenderBuffer = buffer;

			RequestRedraw();
		}

		void LoadScene<T>() where T : Scene, new()
		{
			PressedScene scene = new PressedScene(new T());
			Profile = Profile with {Scene = scene};
		}
	}
}