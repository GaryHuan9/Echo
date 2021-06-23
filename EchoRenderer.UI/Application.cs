using System.Diagnostics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures;
using EchoRenderer.UI.Core;
using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Core.Fields;
using EchoRenderer.UI.Interface;
using SFML.Graphics;
using SFML.Window;

namespace EchoRenderer.UI
{
	public class Application : RenderWindow
	{
		public Application() : base(VideoMode.DesktopMode, nameof(EchoRenderer))
		{
			Closed += (_, _) => Close();

			//Create render environment
			Int2[] resolutions =
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			engine = new ProgressiveRenderEngine();
			buffer = new RenderBuffer(resolutions[1]);

			Scene scene = new TestTexture(); //Creates/loads scene to render

			profile = new ProgressiveRenderProfile
					  {
						  RenderBuffer = buffer,
						  Scene = new PressedScene(scene),
						  Method = new PathTraceWorker(),
						  WorkerSize = 20,
						  EpochSample = 6
					  };

			stopwatch = Stopwatch.StartNew();

			//Create UI
			root = new RootUI(this)
				   {
					   new HierarchyUI(),
					   new SceneViewUI(),
					   new InspectorUI()
				   };

			// 	.Add
			// (
			// 	new RenderPreviewUI
			// 	{
			// 		transform = {LeftPercent = 0.2f, RightPercent = 0.2f, UniformMargins = 10f},
			// 		RenderBuffer = buffer
			// 	}
			// );
		}

		public readonly ProgressiveRenderEngine engine;
		public readonly ProgressiveRenderProfile profile;
		public readonly RenderBuffer buffer;

		public double TotalTime { get; private set; }
		public double DeltaTime { get; private set; }

		readonly RootUI root;
		readonly Stopwatch stopwatch;

		public void Start()
		{
			UpdateTime();

			SetVerticalSyncEnabled(true);
			root.Resize(Size.Cast());
		}

		public void Update()
		{
			UpdateTime();

			switch (engine.CurrentState)
			{
				case ProgressiveRenderEngine.State.waiting:
				{
					engine.Begin(profile);
					break;
				}
			}

			root.Update();
			root.Draw(this);
		}

		void UpdateTime()
		{
			double time = stopwatch.Elapsed.TotalSeconds;

			DeltaTime = time - TotalTime;
			TotalTime = time;
		}

		static void Main()
		{
			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			Application application = new Application();

			application.Start();

			while (application.IsOpen)
			{
				application.DispatchEvents();
				application.Clear();

				application.Update();
				application.Display();
			}

			application.engine.Stop();
			application.engine.Dispose();
		}
	}
}