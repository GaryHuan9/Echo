using System;
using System.Drawing;
using System.Drawing.Imaging;
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
				Camera camera = new Camera(110f) {Angles = new Float2(0f, 0f)};

				scene.Cubemap = new Cubemap("Assets/Cubemaps/OutsideDayTime");
				//scene.Cubemap = new Cubemap("Assets/Cubemaps/DebugCubemap");

				scene.children.Add(new SphereObject(0.5f) {Position = new Float3(1f, -0.4f, 3f)});
				scene.children.Add(new SphereObject(0.3f) {Position = new Float3(-2.3f, 0.7f, 2.6f)});
				scene.children.Add(new BoxObject(new Float3(1f, 0.3f, 1.4f)) {Position = new Float3(0.1f, 0.3f, 2.3f), Rotation = new Float2(30f, 77f)});
				scene.children.Add(new BoxObject(new Float3(0.8f, 0.24f, 0.9f)) {Position = new Float3(-0.3f, -0.6f, 1.3f), Rotation = new Float2(-10f, -6f)});
				scene.children.Add(new SphereObject(0.1f) {Position = new Float3(0.31f, -0.2f, 0.26f)});

				//Render
				//Int2 resolution = new Int2(854, 480); //Half million pixels
				//Int2 resolution = new Int2(1000, 1000);
				//Int2 resolution = new Int2(1920, 1080); //1080p
				Int2 resolution = new Int2(3840, 2160); //2160p

				Renderer renderer = new Renderer(scene, camera, resolution);
				Shade[] colors = new Shade[renderer.bufferSize];

				PerformanceTest renderTest = new PerformanceTest();
				using (renderTest.Start()) renderer.Render(colors, true);

				Console.WriteLine(renderTest.ElapsedMilliseconds);

				//Export
				using Bitmap bitmap = new Bitmap(resolution.x, resolution.y);
				int index = 0;

				foreach (Int2 pixel in resolution.Loop())
				{
					Shade shade = colors[index++];
					Color color = Color.FromArgb(shade.A, shade.R, shade.G, shade.B);

					bitmap.SetPixel(pixel.x, resolution.y - pixel.y - 1, color);
				}

				bitmap.Save("render.png", ImageFormat.Png);
			}

			Console.WriteLine($"Finished in {test.ElapsedMilliseconds}ms");
		}
	}
}