using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
using ForceRenderer.Renderers;
using ForceRenderer.Scenes;

namespace ForceRenderer
{
	class Program
	{
		static void Main()
		{
			PerformanceTest test = new PerformanceTest();

			using (test.Start())
			{
				//Create scene
				Scene scene = new Scene();
				Camera camera = new Camera(110f) {Position = new Float3(0f, 0f, 0f), Angles = new Float2(0f, 0f)};

				scene.Cubemap = new Cubemap("Assets/Cubemaps/OutsideDayTime");
				//scene.Cubemap = new Cubemap("Assets/Cubemaps/DebugCubemap");
				//scene.Cubemap = new Cubemap("Assets/Cubemaps/OutsideSea");

				scene.children.Add(new SphereObject(0.5f) {Position = new Float3(1f, -0.4f, 3f)});
				scene.children.Add(new SphereObject(0.3f) {Position = new Float3(-2.3f, 0.7f, 2.6f)});
				scene.children.Add(new BoxObject(new Float3(1f, 0.3f, 2.4f)) {Position = new Float3(0.1f, 0.3f, 2.3f), Rotation = new Float2(30f, 47f)});
				scene.children.Add(new BoxObject(new Float3(0.8f, 0.24f, 0.9f)) {Position = new Float3(-0.2f, -0.6f, 1.3f), Rotation = new Float2(-10f, 6f)});
				scene.children.Add(new SphereObject(0.1f) {Position = new Float3(0.31f, -0.2f, 0.26f)});

				//scene.children.Add(new BoxObject(new Float3(1f, 1f, 1f)) {Position = new Float3(-0.2f, -0.9f, 1.8f), Rotation = new Float2(20f, 20f)});

				//scene.children.Add(new InfiniteSphereObject(Float3.one, 0.25f) {Position = Float3.half});

				// for (int i = 0; i < 10; i++) scene.children.Add(new SphereObject(0.45f) {Position = new Float3(-2f, -2f, 0f) + new Float3(0.7f, 0.7f, 0.4f) * i});
				//
				// scene.children.Add(new BoxObject(Float3.one) {Position = Float3.forward});
				// scene.children.Add(new BoxObject(Float3.one) {Position = Float3.backward, Rotation = new Float2(0f, -2f)});

				//Render
				//Int2 resolution = new Int2(854, 480); //Half million pixels
				//Int2 resolution = new Int2(1000, 1000);
				//Int2 resolution = new Int2(1920, 1080); //1080p
				Int2 resolution = new Int2(3840, 2160); //2160p

				Renderer renderer = new Renderer(scene, camera, resolution);
				Shade[] buffer = new Shade[renderer.pixelCount];

				renderer.RenderBuffer = buffer;
				renderer.Begin();

				while (!renderer.Completed)
				{
					Console.CursorVisible = false;
					Thread.Sleep(300);

					Console.Write($"\r{renderer.CompletedPixelCount} / {renderer.pixelCount} Pixels Rendered");
				}

				Console.WriteLine();
				renderer.WaitForRender();

				//Export
				using Bitmap bitmap = new Bitmap(resolution.x, resolution.y);
				int index = 0;

				foreach (Int2 pixel in resolution.Loop())
				{
					Shade shade = buffer[index++];
					Color color = Color.FromArgb(shade.A, shade.R, shade.G, shade.B);

					bitmap.SetPixel(pixel.x, resolution.y - pixel.y - 1, color);
				}

				bitmap.Save("render.png", ImageFormat.Png);
			}

			Console.WriteLine($"Completed in {test.ElapsedMilliseconds}ms");
		}
	}
}