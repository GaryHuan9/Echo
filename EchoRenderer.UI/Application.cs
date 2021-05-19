using System.Diagnostics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.Tiles;
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

			engine = new RenderEngine();
			buffer = new RenderBuffer(resolutions[1]);

			RenderProfile profile = pathTraceFastProfile; //Selects or creates render profile

			profile.Scene = new RandomSpheres(120); //Creates/loads scene to render
			profile.RenderBuffer = buffer;

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
					)
				)
			).Add
			(
				new AreaUI {transform = {RightPercent = 0.8f, UniformMargins = 10f}}.Add
				(
					new LabelUI
					{
						transform = {BottomPercent = 1f, BottomMargin = -20f},
						Text = "Hello World"
					}
				).Add
				(
					new ButtonUI
					{
						transform = {BottomPercent = 1f, TopMargin = 30f, BottomMargin = -50f},
						label = {Text = "Button"}
					}
				).Add
				(
					new SliderUI
					{
						transform = {BottomPercent = 1f, TopMargin = 60f, BottomMargin = -80f}
					}
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

		public readonly RenderEngine engine;
		public readonly RenderBuffer buffer;

		public double TotalTime { get; private set; }
		public double DeltaTime { get; private set; }

		readonly RootUI root;
		readonly Stopwatch stopwatch;

		static readonly RenderProfile pathTraceFastProfile = new()
															 {
																 Method = new PathTraceWorker(),
																 TilePattern = new CheckerboardPattern(),
																 PixelSample = 16,
																 AdaptiveSample = 80
															 };

		static readonly RenderProfile pathTraceProfile = new()
														 {
															 Method = new PathTraceWorker(),
															 TilePattern = new CheckerboardPattern(),
															 PixelSample = 32,
															 AdaptiveSample = 400
														 };

		static readonly RenderProfile pathTraceExportProfile = new()
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
				case RenderEngine.State.waiting:
				{
					// engine.Begin();
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