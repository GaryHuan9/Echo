using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
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
			// var source = new Texture("render.png");
			// var destination = new Texture(source.size);
			//
			// var denoiser = new Denoiser(source, destination);
			// denoiser.Dispatch();
			//
			// destination.SaveFile("denoised.png");
			//
			// return;

			// AxisAlignedBoundingBox[] aabbs =
			// {
			// 	new AxisAlignedBoundingBox(Float3.up, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.one * 2, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.right * 2, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.down * 2, Float3.half)
			// };
			//
			// var bvh = new BoundingVolumeHierarchy(null, aabbs, Enumerable.Range(0, aabbs.Length).ToList());
			//
			// var aabb = new AxisAlignedBoundingBox(Float3.zero, new Float3(0.5f, 0f, 0.5f));
			// Ray ray = new Ray(Float3.up, Float3.down);
			//
			// Console.WriteLine(aabb.Intersect(ray));
			//
			// return;

			Float3 zero = new Float3(1f, 0f, 0f);
			Float3 one = new Float3(0f, 0f, 1f);

			Float3 half = zero.Lerp(one, 0.5f);
			Console.WriteLine(half);

			half = half.Normalized;
			Console.WriteLine(half);

			Float3 quarter = zero.Lerp(half, 0.5f);
			Console.WriteLine(quarter);

			quarter = quarter.Normalized;
			Console.WriteLine(quarter);

			Console.WriteLine(zero.Lerp(one, 0.25f));
			Console.WriteLine(zero.Lerp(one, 0.25f).Normalized);

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

			scene.children.Add(new Camera(90f) {Position = new Float3(0f, 12f, 0), Rotation = new Float3(90f, 0f, 0f)});
			// scene.children.Add(new Camera(110f) {Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f)});
			// scene.children.Add(new DirectionalLight {Intensity = new Float3(0.9f, 0.9f, 0.9f), Rotation = new Float3(60f, 90f, 0f)});

			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
			//scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/DebugCubemap");
			scene.Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");

			Mesh bunny = new Mesh("Assets/Models/StanfordBunny/bunnyLP.obj");
			Mesh kunai = new Mesh("Assets/Models/Kunai/wraith_kunai.obj");
			// Mesh bmw = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");

			Console.WriteLine("Assets Loaded");

			Material materialConcrete = new Material {Albedo = new Float3(0.75f, 0.75f, 0.75f), Specular = new Float3(0.03f, 0.03f, 0.03f), Smoothness = 0.11f};
			Material materialChrome = new Material {Albedo = new Float3(0.4f, 0.4f, 0.4f), Specular = new Float3(0.775f, 0.775f, 0.775f), Smoothness = 0.92f};
			Material materialGold = new Material {Albedo = new Float3(0.346f, 0.314f, 0.0903f), Specular = new Float3(0.797f, 0.724f, 0.208f), Smoothness = 0.78f};
			Material materialSmooth = new Material {Albedo = new Float3(0f, 0f, 0f), Specular = new Float3(1f, 1f, 1f), Smoothness = 3f};
			Material materialBlack = new Material {Albedo = new Float3(0.1f, 0.1f, 0.1f), Specular = new Float3(0.5f, 0.5f, 0.5f), Smoothness = 0.9f};

			// scene.children.Add(new TriangleObject(materialGold, new Float3(0f, 0f, 0f), new Float3(1f, 1f, 0f), new Float3(1f, 0f, 0f)) {Position = new Float3(0f, 1f, 0f), Rotation = new Float3(-45f, 0f, 60f), Scale = Float3.one / 10f});

			scene.children.Add(new TriangleObject(materialChrome, new Float3(-12f, 0f, -8f), new Float3(12f, 0f, 8f), new Float3(12f, 0f, -8f)) {Normal0 = Float3.right, Normal1 = Float3.up, Normal2 = Float3.up});
			scene.children.Add(new TriangleObject(materialConcrete, new Float3(-12f, 0f, -8f), new Float3(-12f, 0f, 8f), new Float3(12f, 0f, 8f)) {Normal0 = Float3.right, Normal1 = Float3.right, Normal2 = Float3.up});

			// scene.children.Add(new MeshObject(materialGold, bunny) {Position = new Float3(0f, 0f, -2f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)3f});
			scene.children.Add(new MeshObject(materialChrome, kunai) {Position = new Float3(0f, -10f, -3f), Rotation = new Float3(0f, 90f, 0f), Scale = (Float3)2.5f});
			// scene.children.Add(new MeshObject(materialBlack, bmw) {Position = Float3.zero, Rotation = new Float3(0f, -65f, 0f), Scale = (Float3)1.4f});

			// MinMaxInt range = new MinMaxInt(-3, 1);
			//
			// foreach (int index in range.Loop())
			// {
			// 	Material material = new Material {Albedo = Float3.one, Specular = (Float3)range.InverseLerp(index), Smoothness = 0.85f};
			// 	Float3 position = new Float3(2.8f, 0f, -0.8f) * index + new Float3(1.7f, 0f, 0.2f);
			//
			// 	scene.children.Add(new MeshObject(material, bmw) {Position = position, Rotation = new Float3(0f, -60f, 0f)});
			// }

			//FillRandomSpheres(scene, 200);
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
											PixelSample = 1, TileSize = 100
										};

			renderDisplay.Engine = engine;
			engine.Begin();

			engine.WaitForRender();

			for (int y = 0; y < 10; y++)
			{
				for (int x = 0; x < buffer.size.x; x++)
				{
					buffer.SetPixel(new Int2(x, y), (Color32)Float3.zero.Lerp(Float3.one, (float)x / buffer.size.x));
				}
			}

			buffer.SaveFile("render.png");

			Texture noisy = new Texture(buffer.size);
			var denoiser = new Denoiser(buffer, noisy);

			denoiser.Dispatch();
			noisy.SaveFile("noisy.png");

			commandsController.Log($"Completed in {engine.Elapsed.TotalMilliseconds}ms");
		}

		static Material GetRandomMetallic()
		{
			Float3 color = new Float3(RandomHelper.Range(0.5f, 1f), RandomHelper.Range(0.5f, 1f), RandomHelper.Range(0.5f, 1f));
			return new Material {Albedo = color, Specular = color, Smoothness = RandomHelper.Range(0.6f, 0.95f)};
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
				bool emissive = RandomHelper.Value < 0.1d;

				Material material = new Material
									{
										Albedo = color,
										Specular = metal ? color : Float3.one * 0.05f,
										Emission = emissive ? bright : Float3.zero,
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