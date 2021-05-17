using System.Threading;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.Tiles;
using EchoRenderer.Textures;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Texture = SFML.Graphics.Texture;

namespace EchoRenderer.GUI
{
	class Application : RenderWindow
	{
		public Application() : base(VideoMode.DesktopMode, nameof(EchoRenderer))
		{
			Closed += (_, _) => Close();
			KeyPressed += OnKeyPressed;

			//Create render environment
			Int2[] resolutions =
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			engine = new RenderEngine();
			buffer = new RenderBuffer(resolutions[1]);

			RenderProfile profile = pathTraceProfile; //Selects or creates render profile

			profile.Scene = new SingleMaterialBall(); //Creates/loads scene to render
			profile.RenderBuffer = buffer;

			engine.Profile = profile;

			//Assign drawing sprite
			uint width = (uint)buffer.size.x;
			uint height = (uint)buffer.size.y;

			pixels = new byte[width * height * 4];
			textureGUI = new Texture(width, height);
			spriteGUI = new Sprite(textureGUI);
		}

		public readonly RenderEngine engine;
		public readonly RenderBuffer buffer;

		readonly byte[] pixels;
		readonly Texture textureGUI;
		readonly Sprite spriteGUI;

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

		public void Update()
		{
			switch (engine.CurrentState)
			{
				case RenderEngine.State.waiting:
				{
					engine.Begin();
					break;
				}
				case RenderEngine.State.rendering:
				case RenderEngine.State.paused:
				case RenderEngine.State.completed:
				case RenderEngine.State.aborted:
				{
					foreach (Int2 position in buffer.size.Loop())
					{
						Color32 pixel = (Color32)buffer[position];
						int index = buffer.ToIndex(position) * 4;

						pixels[index + 0] = pixel.r;
						pixels[index + 1] = pixel.g;
						pixels[index + 2] = pixel.b;
						pixels[index + 3] = pixel.a;
					}

					textureGUI.Update(pixels);
					Draw(spriteGUI);

					break;
				}
			}
		}

		void OnKeyPressed(object sender, KeyEventArgs argument) { }

		static void Main()
		{
			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			Application application = new Application();

			while (application.IsOpen)
			{
				application.DispatchEvents();
				application.Clear();

				application.Update();
				application.Display();
			}

			application.buffer.Save("renderGUI.png");

			// Font font = new Font("Assets/Fonts/JetBrainsMono/JetBrainsMono-Bold.ttf");
			// Text text = new Text("Hello World!", font);
			// text.CharacterSize = 40;
			// float textWidth = text.GetLocalBounds().Width;
			// float textHeight = text.GetLocalBounds().Height;
			// float xOffset = text.GetLocalBounds().Left;
			// float yOffset = text.GetLocalBounds().Top;
			// text.Origin = new Vector2f(textWidth / 2f + xOffset, textHeight / 2f + yOffset);
			// text.Position = new Vector2f(window.Size.X / 2f, window.Size.Y / 2f);
		}
	}
}