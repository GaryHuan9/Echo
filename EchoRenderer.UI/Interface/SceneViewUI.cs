using System;
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
			transform.HorizontalPercents = 0.2f;
			transform.UniformMargins = 10f;

			engine = new ProgressiveRenderEngine();
			renderPreview = new RenderPreviewUI();

			Add(renderPreview);

			//Create render environment
			Scene scene = new TestMaterials();

			Profile = new ProgressiveRenderProfile
					  {
						  Scene = new PressedScene(scene),
						  Method = new PathTraceWorker(),
						  WorkerSize = Environment.ProcessorCount - 2,
						  EpochSample = 6
					  };
		}

		ProgressiveRenderProfile _profile;

		public ProgressiveRenderProfile Profile
		{
			get => _profile;
			set => _profile = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		readonly ProgressiveRenderEngine engine;
		readonly RenderPreviewUI renderPreview;

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
					catch (Exception exception)
					{
						Console.WriteLine(exception);
					}

					break;
				}
			}
		}

		protected override void Reorient(Float2 position, Float2 size)
		{
			base.Reorient(position, size);
			Int2 resolution = size.Rounded;

			resolution -= Int2.one - resolution % 2; //TODO: Temporary! Remove

			if (resolution == Profile.RenderBuffer?.size) return;
			var buffer = new ProgressiveRenderBuffer(resolution);

			Profile = Profile with {RenderBuffer = buffer};
			renderPreview.RenderBuffer = buffer;

			engine.Stop();
		}

		public override void Dispose()
		{
			base.Dispose();

			engine.Stop();
			engine.Dispose();
		}
	}
}