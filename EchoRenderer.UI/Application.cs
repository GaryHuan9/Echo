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

			Scene scene = new GridSpheres(); //Creates/loads scene to render

			profile = new ProgressiveRenderProfile
					  {
						  RenderBuffer = buffer,
						  Scene = new PressedScene(scene),
						  Method = new PathTraceWorker(),
						  WorkerSize = 20,
						  EpochSample = 6
					  };

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
						(tempField = new Float3FieldUI { }).Label("Camera Position")
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

		public readonly ProgressiveRenderEngine engine;
		public readonly ProgressiveRenderProfile profile;
		public readonly RenderBuffer buffer;

		public double TotalTime { get; private set; }
		public double DeltaTime { get; private set; }

		readonly RootUI root;
		readonly Stopwatch stopwatch;

		readonly Float3FieldUI tempField;

		public void Start()
		{
			UpdateTime();

			SetVerticalSyncEnabled(true);
			root.Resize(Size.Cast());

			tempField.Value = profile.Scene.camera.Position;
		}

		public void Update()
		{
			UpdateTime();

			Camera camera = profile.Scene.camera;

			Float3 old = camera.Position;
			Float3 newValue = tempField.Value;

			if (old != newValue)
			{
				camera.Position = newValue;
				engine.Stop();
			}

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