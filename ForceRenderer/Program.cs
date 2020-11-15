using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Lights;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Renderers;
using ForceRenderer.Terminals;

namespace ForceRenderer
{
	public class Program
	{
		static void Main()
		{
			// ThreadHelper.MainThread = Thread.CurrentThread;
			//
			// // var matrix = new Float4x4
			// // (
			// // 	00f, 01f, 02f, 03f,
			// // 	10f, 11f, 12f, 13f,
			// // 	20f, 21f, 22f, 23f,
			// // 	30f, 31f, 32f, 33f
			// // );
			//
			// Console.WriteLine(GetRandom());
			//
			// static Float4x4 GetRandom() => new Float4x4
			// (
			// 	(float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value,
			// 	(float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value,
			// 	(float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value,
			// 	(float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value
			// );
			//
			// return;

			Terminal terminal = new Terminal();

			var commandsController = new CommandsController(terminal);
			var renderDisplay = new RenderMonitor(terminal);

			terminal.AddSection(commandsController);
			terminal.AddSection(renderDisplay);

			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			//Create scene
			Scene scene = new Scene();

			//scene.children.Add(new Camera(120f) {Position = new Float3(0f, 1f, -2), Rotation = new Float2(0f, 0f, 0f)});
			scene.children.Add(new Camera(90f) {Position = new Float3(0f, 2f, -4.5f), Rotation = new Float3(25f, 0f, 0f)});
			scene.children.Add(new DirectionalLight {Intensity = new Float3(0.9f, 0.9f, 0.9f), Rotation = new Float3(60f, 80f, 0f)});

			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/DebugCubemap");
			scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");
			//scene.Cubemap = new CylindricalCubemap("Assets/Cubemaps/CapeHill.png"); //Bad quality

			Material materialConcrete = new Material {Albedo = new Float3(0.75f, 0.75f, 0.75f), Specular = new Float3(0.03f, 0.03f, 0.03f)};
			Material materialChrome = new Material {Albedo = new Float3(0.4f, 0.4f, 0.4f), Specular = new Float3(0.775f, 0.775f, 0.775f)};
			Material materialGold = new Material {Albedo = new Float3(0.346f, 0.314f, 0.0903f), Specular = new Float3(0.797f, 0.724f, 0.208f)};

			scene.children.Add(new TriangleObject(materialConcrete, new Float3(-5f, 0f, -5f), new Float3(5f, 0f, -5f), new Float3(5f, 0f, 5f)));
			scene.children.Add(new TriangleObject(materialChrome, new Float3(-5f, 0f, -5f), new Float3(5f, 0f, 5f), new Float3(-5f, 0f, 5f)));

			FillRandomSpheres(scene, 80);

			//Render
			Int2[] resolutions =
			{
				new Int2(320, 180), new Int2(854, 480), new Int2(1000, 1000), new Int2(1920, 1080), new Int2(3840, 2160)
			};

			Texture buffer = new Texture(resolutions[3]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = scene,
											PixelSample = 1024, TileSize = 128
										};

			renderDisplay.Engine = engine;
			engine.Begin();

			engine.WaitForRender();
			buffer.SaveFile("render.png");

			Console.WriteLine($"Completed in {engine.Elapsed.TotalMilliseconds}ms");
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

				scene.children.Add(new SphereObject(material, radius) {Position = position, Rotation = position * 100f});
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