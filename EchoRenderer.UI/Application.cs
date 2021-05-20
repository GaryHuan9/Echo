using System.Diagnostics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Engines.Tiles;
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
		public Application() : base(new VideoMode(1920, 1080) /*VideoMode.DesktopMode*/, nameof(EchoRenderer))
		{
			Closed += (_, _) => Close();

			//Create render environment
			Int2[] resolutions =
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			engine = new TiledRenderEngine();
			buffer = new RenderBuffer(resolutions[1]);

			TiledRenderProfile profile = pathTraceFastProfile; //Selects or creates render profile
			Scene scene = new RandomSpheres(120);              //Creates/loads scene to render

			profile = profile with
					  {
						  RenderBuffer = buffer,
						  Scene = new PressedScene(scene)
					  };

			engine.Profile = profile;
			stopwatch = Stopwatch.StartNew();

			//Test
			root = (RootUI)new RootUI(this).Add
			(
				new AreaUI {transform = {LeftPercent = 0.8f, UniformMargins = 10f}}.Add
				(
					new AutoLayoutAreaUI { }.Add
					(
						new LabelUI
						{
							Text = "Hello World 1",
							Align = LabelUI.Alignment.left
						}
					).Add
					(
						new ButtonUI
						{
							label =
							{
								Text = "Button 1",
								Align = LabelUI.Alignment.right
							}
						}
					).Add
					(
						new LabelUI
						{
							Text = "Hello World 2 pp"
						}
					).Add
					(
						new ButtonUI
						{
							label = {Text = "Button 2"}
						}
					).Add
					(
						new ButtonUI
						{
							label = {Text = "Button 3"}
						}
					).Add
					(
						new TextFieldUI {Text = "Test Field Hehe"}
					).Add
					(
						new FloatFieldUI { }
					).Add
					(
						new Float3FieldUI { }
					)
				)
			).Add
			(
				new AreaUI {transform = {RightPercent = 0.8f, UniformMargins = 10f}}.Add
				(
					new AutoLayoutAreaUI { }.Add
					(
						new ButtonUI
						{
							label = {Text = "Button"}
						}.Label("Click Me")
					).Add
					(
						new TextFieldUI {Text = "Test Field Here"}.Label("Type me")
					).Add
					(
						new FloatFieldUI { }.Label("Sample Count")
					).Add
					(
						new Float3FieldUI { }.Label("Camera Position")
					)
				)
			).Add
			(
				new RenderPreviewUI
				{
					transform = {LeftPercent = 0.2f, RightPercent = 0.2f, UniformMargins = 10f},
					RenderBuffer = buffer
				}
			);
		}

		public readonly TiledRenderEngine engine;
		public readonly RenderBuffer buffer;

		public double TotalTime { get; private set; }
		public double DeltaTime { get; private set; }

		readonly RootUI root;
		readonly Stopwatch stopwatch;

		static readonly TiledRenderProfile pathTraceFastProfile = new()
																  {
																	  Method = new PathTraceWorker(),
																	  TilePattern = new CheckerboardPattern(),
																	  PixelSample = 16,
																	  AdaptiveSample = 80
																  };

		static readonly TiledRenderProfile pathTraceProfile = new()
															  {
																  Method = new PathTraceWorker(),
																  TilePattern = new CheckerboardPattern(),
																  PixelSample = 32,
																  AdaptiveSample = 400
															  };

		static readonly TiledRenderProfile pathTraceExportProfile = new()
																	{
																		Method = new PathTraceWorker(),
																		TilePattern = new CheckerboardPattern(),
																		PixelSample = 64,
																		AdaptiveSample = 1600
																	};

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
				case TiledRenderEngine.State.waiting:
				{
					engine.Begin();
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
		}
	}
}