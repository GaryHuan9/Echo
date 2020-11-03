using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
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
			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			PerformanceTest test = new PerformanceTest();

			using (test.Start())
			{
				//Create scene
				Scene scene = new Scene();

				//scene.children.Add(new Camera(120f) {Position = new Float3(0f, 1f, -2), Rotation = new Float2(0f, 0f)});
				scene.children.Add(new Camera(90f) {Position = new Float3(0f, 2f, -4.5f), Rotation = new Float2(25f, 0f)});
				scene.children.Add(new DirectionalLight {Intensity = new Float3(0.9f, 0.9f, 0.9f), Rotation = new Float2(60f, 80f)});

				scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
				//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/DebugCubemap");
				//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");
				//scene.Cubemap = new CylindricalCubemap("Assets/Cubemaps/CapeHill.png"); //Bad quality

				Material materialChrome = new Material {Albedo = new Float3(0.4f, 0.4f, 0.4f), Specular = new Float3(0.775f, 0.775f, 0.775f)};
				Material materialGold = new Material {Albedo = new Float3(0.346f, 0.314f, 0.0903f), Specular = new Float3(0.797f, 0.724f, 0.208f)};
				Material materialConcrete = new Material {Albedo = new Float3(0.75f, 0.75f, 0.75f), Specular = new Float3(0.03f, 0.03f, 0.03f)};

				scene.children.Add(new PlaneObject(materialConcrete));

				scene.children.Add(new BoxObject(materialGold, Float3.one) {Position = new Float3(-6f, 0.5f, 6f)});
				scene.children.Add(new BoxObject(materialChrome, Float3.one) {Position = new Float3(6f, 0.5f, 6f)});

				FillRandomSpheres(scene, 80);

				// scene.children.Add(new SphereObject(materialGold, 0.5f) {Position = new Float3(1f, 0.6f, 1f)});
				// scene.children.Add(new SphereObject(materialChrome, 0.3f) {Position = new Float3(-2.3f, 1.7f, 0.6f)});
				// scene.children.Add(new SphereObject(materialChrome, 0.1f) {Position = new Float3(0.32f, 0.8f, -1.73f)});
				// scene.children.Add(new BoxObject(materialChrome, new Float3(1f, 0.3f, 2.4f)) {Position = new Float3(0.1f, 1.3f, 0.54f), Rotation = new Float2(30f, 47f)});
				// scene.children.Add(new BoxObject(materialChrome, new Float3(0.8f, 0.24f, 0.9f)) {Position = new Float3(-0.6f, 0.36f, 0.3f), Rotation = new Float2(-20f, 16f)});
				// scene.children.Add(new BoxObject(materialGold, new Float3(0.5f, 0.5f, 0.5f)) {Position = new Float3(-1.5f, 0.25f, -0.5f), Rotation = new Float2(0f, 0f)});

				//Render
				Int2[] resolutions =
				{
					new Int2(320, 180), new Int2(854, 480), new Int2(1000, 1000),
					new Int2(1000, 1000), new Int2(1920, 1080), new Int2(3840, 2160)
				};

				Renderer renderer = new Renderer(scene, 32);
				Texture buffer = new Texture(resolutions[4]);

				renderer.RenderBuffer = buffer;
				renderer.Begin();

				while (!renderer.Completed)
				{
					Console.CursorVisible = false;
					Console.Write($"\r{renderer.CompletedPixelCount} / {renderer.RenderLength} Pixels Rendered");
				}

				Console.WriteLine();
				renderer.WaitForRender();

				Texture denoised = new Texture(buffer.size);
				Denoiser denoiser = new Denoiser(buffer, denoised);

				denoiser.Dispatch();
				denoised.SaveFile("render.png");
			}

			Console.WriteLine($"Completed in {test.ElapsedMilliseconds}ms");
		}

		static void FillRandomSpheres(Scene scene, int count)
		{
			MinMax radiusRange = new MinMax(0.2f, 0.5f);
			MinMax positionRange = new MinMax(0f, 3f);

			for (int i = 0; i < count; i++)
			{
				//Orientation
				float radius;
				Float3 position;

				do
				{
					radius = radiusRange.RandomValue;
					position = new Float3(positionRange.RandomValue, radius, 0f).RotateXZ(RandomHelper.Range(360f));
				}
				while (IntersectingOthers(radius, position));

				//Material
				Float3 color = new Float3((float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value);

				bool metal = RandomHelper.Value < 0d;
				Material material = new Material
									{
										Albedo = metal ? Float3.zero : color,
										Specular = metal ? color : Float3.one * 0.05f
									};

				scene.children.Add(new SphereObject(material, radius) {Position = position});
			}

			bool IntersectingOthers(float radius, Float3 position)
			{
				for (int i = 0; i < scene.children.Count; i++)
				{
					var sphere = scene.children[i] as SphereObject;
					if (sphere == null) continue;

					float distance = sphere.Radius + radius;
					if ((sphere.Position - position).SquaredMagnitude <= distance * distance) return true;
				}

				return false;
			}
		}
	}
}