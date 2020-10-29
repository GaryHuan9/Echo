using System;
using System.Drawing;
using System.Drawing.Imaging;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Modifiers;
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
				Camera camera = new Camera(110f) {Angles = new Float2(-20f, 160f)};

				using Cubemap cubemap = new Cubemap("Assets/Cubemaps/DebugCubemap");
				scene.Cubemap = cubemap;

				scene.AddSceneObject(new SphereObject(0.5f) {Position = new Float3(1f, -0.4f, 3f)});
				scene.AddSceneObject(new SphereObject(0.3f) {Position = new Float3(-2.3f, 0.7f, 2.6f)});
				scene.AddSceneObject(new BoxObject(new Float3(1f, 0.3f, 1.4f)) {Position = new Float3(0.1f, 0.3f, 2.3f)});
				scene.AddSceneObject(new BoxObject(new Float3(1f, 0.3f, 1.4f)) {Position = new Float3(0.1f, -0.4f, 2.3f)});
				scene.AddSceneObject(new RepetitionModifier(new SphereObject(0.1f), (Float3)1f) {Position = new Float3(0.31f, -0.5f, 0.26f)});

				//Render
				Int2 resolution = new Int2(854, 480); //Half million pixels
				//Int2 resolution = new Int2(1000, 1000);
				//Int2 resolution = new Int2(1920, 1080); //1080p
				//Int2 resolution = new Int2(3840, 2160); //2160p

				Renderer renderer = new Renderer(scene, camera, resolution);
				Float3[] colors = new Float3[renderer.bufferSize];

				renderer.Render(colors, true);

				//Export
				using Bitmap bitmap = new Bitmap(resolution.x, resolution.y);
				int index = 0;

				foreach (Int2 pixel in resolution.Loop())
				{
					Int3 color = (colors[index++].Clamp(0f, 1f - Scalars.Epsilon) * 256f).Floored;
					bitmap.SetPixel(pixel.x, resolution.y - pixel.y - 1, Color.FromArgb(color.x, color.y, color.z));
				}

				bitmap.Save("render.png", ImageFormat.Png);
			}

			Console.WriteLine($"Finished in {test.ElapsedMilliseconds}ms");
		}
	}
}