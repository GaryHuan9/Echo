using System;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Lights;
using ForceRenderer.Objects.SceneObjects;
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

				scene.children.Add(new Camera(110f) {Position = new Float3(0f, 1f, -2), Rotation = new Float2(0f, 0f)});
				//scene.children.Add(new Camera(90f) {Position = new Float3(0f, 3f, -3f), Rotation = new Float2(45f, 0f)});
				scene.children.Add(new DirectionalLight {Rotation = new Float2(60f, 0f)});

				//scene.Cubemap = new Cubemap("Assets/Cubemaps/OutsideDayTime");
				//scene.Cubemap = new Cubemap("Assets/Cubemaps/DebugCubemap");
				scene.Cubemap = new Cubemap("Assets/Cubemaps/OutsideSea");

				// scene.children.Add(new BoxObject(new Float3(1f, 1f, 1f)) {Position = new Float3(-1.5f, 0.5f, 0f), Rotation = new Float2(0f, 15f)});
				// scene.children.Add(new SphereObject(0.5f) {Position = new Float3(1.5f, 0.5f, 0f)});

				scene.children.Add(new PlaneObject());
				scene.children.Add(new HexagonalPrismObject(1f, 0.3f) {Position = new Float3(-2.2f, 0.3f, 1.4f), Rotation = new Float2(20f, 60f)});
				scene.children.Add(new SphereObject(0.5f) {Position = new Float3(1f, 0.6f, 1f)});
				scene.children.Add(new SphereObject(0.3f) {Position = new Float3(-2.3f, 1.7f, 0.6f)});
				scene.children.Add(new SphereObject(0.1f) {Position = new Float3(0.32f, 0.8f, -1.73f)});
				scene.children.Add(new BoxObject(new Float3(1f, 0.3f, 2.4f)) {Position = new Float3(0.1f, 1.3f, 0.54f), Rotation = new Float2(30f, 47f)});
				scene.children.Add(new BoxObject(new Float3(0.8f, 0.24f, 0.9f)) {Position = new Float3(-0.6f, 0.36f, 0.3f), Rotation = new Float2(-20f, 16f)});

				//Render
				//Int2 resolution = new Int2(854, 480); //Half million pixels
				//Int2 resolution = new Int2(1000, 1000);
				//Int2 resolution = new Int2(1920, 1080); //1080p
				Int2 resolution = new Int2(3840, 2160); //2160p

				Renderer renderer = new Renderer(scene);
				Texture buffer = new Texture(resolution);

				renderer.RenderBuffer = buffer;
				renderer.Begin();

				while (!renderer.Completed)
				{
					Console.CursorVisible = false;
					Console.Write($"\r{renderer.CompletedPixelCount} / {renderer.RenderLength} Pixels Rendered");
				}

				Console.WriteLine();
				renderer.WaitForRender();

				buffer.SaveFile("render.png");
			}

			Console.WriteLine($"Completed in {test.ElapsedMilliseconds}ms");
		}
	}
}