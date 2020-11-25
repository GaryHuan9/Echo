using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
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

			// var source = new Texture("render.png");
			// var destination = new Texture(source.size);
			//
			// var denoiser = new Denoiser(source, destination);
			// denoiser.Dispatch();
			//
			// destination.SaveFile("denoised.png");
			//
			// return;

			AxisAlignedBoundingBox[] aabbs =
			{
				new AxisAlignedBoundingBox(Float3.up, Float3.half),
				new AxisAlignedBoundingBox(Float3.one * 2, Float3.half),
				new AxisAlignedBoundingBox(Float3.right * 2, Float3.half),
				new AxisAlignedBoundingBox(Float3.down * 2, Float3.half)
			};

			var bvh = new BoundingVolumeHierarchy(null, aabbs, Enumerable.Range(0, aabbs.Length).ToList());

			return;

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
			scene.children.Add(new Camera(110f) {Position = new Float3(0f, 3f, -6f), Rotation = new Float3(20f, 0f, 0f)});
			//scene.children.Add(new DirectionalLight {Intensity = new Float3(0.9f, 0.9f, 0.9f), Rotation = new Float3(60f, 90f, 0f)});

			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/DebugCubemap");
			scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");

			Mesh bunny = new Mesh("Assets/Models/bunnyLP.obj");

			Material materialConcrete = new Material {Albedo = new Float3(0.75f, 0.75f, 0.75f), Specular = new Float3(0.03f, 0.03f, 0.03f), Smoothness = 0.11f};
			Material materialChrome = new Material {Albedo = new Float3(0.4f, 0.4f, 0.4f), Specular = new Float3(0.775f, 0.775f, 0.775f), Smoothness = 0.92f};
			Material materialGold = new Material {Albedo = new Float3(0.346f, 0.314f, 0.0903f), Specular = new Float3(0.797f, 0.724f, 0.208f), Smoothness = 0.78f};
			Material materialSmooth = new Material {Albedo = new Float3(0f, 0f, 0f), Specular = new Float3(1f, 1f, 1f), Smoothness = 3f};
			Material materialFakeGold = new Material {Albedo = new Float3(0.797f, 0.724f, 0.208f), Specular = new Float3(0.346f, 0.314f, 0.0903f), Smoothness = 0.13f};

			scene.children.Add(new TriangleObject(materialChrome, new Float3(-8f, 0f, -8f), new Float3(8f, 0f, 8f), new Float3(8f, 0f, -8f)));
			scene.children.Add(new TriangleObject(materialConcrete, new Float3(-8f, 0f, -8f), new Float3(-8f, 0f, 8f), new Float3(8f, 0f, 8f)));

			scene.children.Add(new MeshObject(materialFakeGold, bunny) {Position = new Float3(0f, 0f, -2f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)3f});
			//scene.children.Add(new TriangleObject(materialGold, new Float3(0f, 0f, 0f), new Float3(1f, 1f, 0f), new Float3(1f, 0f, 0f)) {Position = new Float3(0f, 1f, 0f), Rotation = new Float3(-45f, 30f, 60f), Scale = new Float3(1f, 10f, 1f)});

			//FillRandomSpheres(scene, 80);
			//FillRandomCubes(scene, 60);

			//Render
			Int2[] resolutions =
			{
				new Int2(320, 180), new Int2(854, 480), new Int2(1920, 1080), new Int2(3840, 2160), new Int2(1000, 1000)
			};

			Texture buffer = new Texture(resolutions[1]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = scene,
											PixelSample = 256, TileSize = 100
										};

			renderDisplay.Engine = engine;
			engine.Begin();

			engine.WaitForRender();
			buffer.SaveFile("render.png");

			Texture noisy = new Texture(buffer.size);
			var denoiser = new Denoiser(buffer, noisy);

			denoiser.Dispatch();
			noisy.SaveFile("noisy.png");

			commandsController.Log($"Completed in {engine.Elapsed.TotalMilliseconds}ms");
		}

		static void FillRandomSpheres(Scene scene, int count)
		{
			MinMax radiusRange = new MinMax(0.1f, 0.5f);
			MinMax positionRange = new MinMax(0f, 5f);

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
				Float3 bright = new Float3(RandomHelper.Range(3f, 8f), RandomHelper.Range(3f, 8f), RandomHelper.Range(3f, 8f));

				bool metal = RandomHelper.Value < 0.5d;
				bool emissive = RandomHelper.Value < 0.2d;

				Material material = new Material
									{
										Albedo = color,
										Specular = metal ? color : Float3.one * 0.05f,
										//Emission = emissive ? bright : Float3.zero,
										Smoothness = (float)RandomHelper.Value / 2f + (metal ? 0.5f : 0f)
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

		static void FillRandomCubes(Scene scene, int count)
		{
			MinMax sizeRange = new MinMax(0.15f, 0.5f);
			MinMax positionRange = new MinMax(0f, 3f);

			List<Float3> positions = new List<Float3>();

			for (int i = 0; i < count; i++)
			{
				//Orientation
				float size;
				Float3 position;

				do
				{
					size = sizeRange.RandomValue;
					position = new Float3(positionRange.RandomValue, size, 0f).RotateXZ(RandomHelper.Range(360f));
				}
				while (IntersectingOthers(size, position));

				//Material
				Float3 color = new Float3((float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value);

				bool metal = RandomHelper.Value < 0.5d;
				Material material = new Material
									{
										Albedo = metal ? Float3.zero : color,
										Specular = metal ? color : Float3.one * 0.05f
									};

				//Add box
				Float3 rotation = Float3.up * RandomHelper.Range(0f, 360f);
				Float3 scale = Float3.one * size;
				positions.Add(position);

				//X axis
				AddFace(new Float3(1f, -1f, -1f), new Float3(1f, -1f, 1f), new Float3(1f, 1f, -1f), new Float3(1f, 1f, 1f));
				AddFace(new Float3(-1f, -1f, 1f), new Float3(-1f, -1f, -1f), new Float3(-1f, 1f, 1f), new Float3(-1f, 1f, -1f));

				//Y axis
				AddFace(new Float3(-1f, 1f, -1f), new Float3(1f, 1f, -1f), new Float3(-1f, 1f, 1f), new Float3(1f, 1f, 1f));
				AddFace(new Float3(1f, -1f, -1f), new Float3(-1f, -1f, -1f), new Float3(1f, -1f, 1f), new Float3(-1f, -1f, 1f));

				//Z axis
				AddFace(new Float3(1f, -1f, 1f), new Float3(-1f, -1f, 1f), new Float3(1f, 1f, 1f), new Float3(-1f, 1f, 1f));
				AddFace(new Float3(-1f, -1f, -1f), new Float3(1f, -1f, -1f), new Float3(-1f, 1f, -1f), new Float3(1f, 1f, -1f));

				void AddFace(Float3 zz, Float3 oz, Float3 zo, Float3 oo)
				{
					scene.children.Add(new TriangleObject(material, zz, oo, oz) {Position = position, Rotation = rotation, Scale = scale});
					scene.children.Add(new TriangleObject(material, zz, zo, oo) {Position = position, Rotation = rotation, Scale = scale});
				}
			}

			bool IntersectingOthers(float size, Float3 position)
			{
				const float Sqrt2 = 1.41421356237f;
				float radius = Sqrt2 * size;

				for (int i = 0; i < positions.Count; i++)
				{
					Float3 cube = positions[i];
					float distance = Sqrt2 * cube.y + radius;
					if ((cube - position).SquaredMagnitude <= distance * distance) return true;
				}

				return false;
			}
		}
	}
}