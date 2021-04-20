using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using EchoRenderer.IO;
using EchoRenderer.Mathematics;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Textures;

namespace EchoRenderer.Objects.Scenes
{
	public class MaterialBallScene : StandardScene
	{
		public MaterialBallScene() : base(new Glossy {Albedo = new Float3(0.78f, 0.76f, 0.79f), Smoothness = 0.74f})
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)1.2f);

			var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

			children.Add(new MeshObject(mesh, materials) {Position = new Float3(0f, 0f, -2.5f), Rotation = Float3.up * -75f, Scale = Float3.one * 2f});
		}
	}

	public class GridMaterialBallScene : Scene
	{
		public GridMaterialBallScene() //5.8 billion triangles
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)1.2f);

			var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

			ObjectPack ball = new ObjectPack();
			Int3 gridSize = new Int3(10, 4, 7);

			ball.children.Add(new MeshObject(mesh, materials) {Rotation = Float3.up * -75f, Scale = (Float3)0.45f});

			Diffuse coreSource = (Diffuse)materials["Core"];
			Glass solidSource = (Glass)materials["Solid"];

			Dictionary<Int2, Material> cores = new Dictionary<Int2, Material>();
			Dictionary<int, Material> solids = new Dictionary<int, Material>();

			foreach (Int3 position in new EnumerableSpace3D(-gridSize, gridSize))
			{
				Float3 percent = Float3.InverseLerp(-gridSize, gridSize, position);

				Material core = cores.TryGetValue(position.YZ) ?? (cores[position.YZ] = new Diffuse {Albedo = Float3.Lerp(Float3.one, Float3.Lerp(coreSource.Albedo, Float3.zero, percent.z), percent.y)});
				Material solid = solids.TryGetValue(position.x) ?? (solids[position.x] = new Glass {Albedo = solidSource.Albedo, IndexOfRefraction = solidSource.IndexOfRefraction, Roughness = percent.x});

				MaterialMapper mapper = new MaterialMapper {[coreSource] = core, [solidSource] = solid};
				children.Add(new ObjectPackInstance(ball) {Position = position.XYZ, Mapper = mapper});
			}

			children.Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f)});

			Camera camera = new Camera(100f) {Position = new Float3(-5f, 6f, -10f)};

			camera.LookAt(Float3.zero);
			children.Add(camera);
		}
	}

	public class BunnyScene : StandardScene
	{
		public BunnyScene()
		{
			var mesh = new Mesh("Assets/Models/StanfordBunny/bunny.obj");
			var materials = new MaterialLibrary("Assets/Models/StanfordBunny/bunny.mat");

			children.Add(new MeshObject(mesh, materials) {Position = new Float3(0f, 0f, -3f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)2.5f});
		}
	}

	public class CornellBox : Scene
	{
		public CornellBox()
		{
			Diffuse green = new Diffuse {Albedo = Utilities.ToColor("00CB21").XYZ};
			Diffuse red = new Diffuse {Albedo = Utilities.ToColor("CB0021").XYZ};
			Diffuse blue = new Diffuse {Albedo = Utilities.ToColor("0021CB").XYZ};

			Diffuse white = new Diffuse {Albedo = Utilities.ToColor("EEEEF2").XYZ};
			Emissive light = new Emissive {Emission = Utilities.ToColor("#FFFAF4").XYZ * 3.4f};

			const float Width = 10f;
			const float Half = Width / 2f;
			const float Size = Half / 5f * 3f;

			children.Add(new PlaneObject(white, (Float2)Width) {Position = Float3.zero, Rotation = Float3.zero});                             //Floor
			children.Add(new PlaneObject(white, (Float2)Width) {Position = new Float3(0f, Width, 0f), Rotation = new Float3(180f, 0f, 0f)});  //Roof
			children.Add(new PlaneObject(blue, (Float2)Width) {Position = new Float3(0f, Half, Half), Rotation = new Float3(-90f, 0f, 0f)});  //Back
			children.Add(new PlaneObject(white, (Float2)Width) {Position = new Float3(0f, Half, -Half), Rotation = new Float3(90f, 0f, 0f)}); //Front

			children.Add(new PlaneObject(green, (Float2)Width) {Position = new Float3(Half, Half, 0f), Rotation = new Float3(0f, 0f, 90f)});        //Right
			children.Add(new PlaneObject(red, (Float2)Width) {Position = new Float3(-Half, Half, 0f), Rotation = new Float3(0f, 0f, -90f)});        //Left
			children.Add(new PlaneObject(light, (Float2)Half) {Position = new Float3(0f, Width - 0.01f, 0f), Rotation = new Float3(180f, 0f, 0f)}); //Light

			children.Add(new BoxObject(white, new Float3(Size, Size, Size)) {Position = new Float3(Size / 1.5f, Size / 2f, -Size / 1.5f), Rotation = Float3.up * 21f});
			children.Add(new BoxObject(white, new Float3(Size, Size * 2f, Size)) {Position = new Float3(-Size / 1.5f, Size, Size / 1.5f), Rotation = Float3.up * -21f});

			const float Radius = 0.4f;
			const int Count = 3;

			Float3 ballWhite = Utilities.ToColor("F7F7FD").XYZ;

			for (int i = -Count; i <= Count; i++)
			{
				float percent = Scalars.InverseLerp(-Count, Count, (float)i);
				Float4 position = new Float4(i * Radius * 2.1f, Width - Radius, Radius - Half, Half - Radius);

				children.Add(new SphereObject(new Glass {Albedo = ballWhite, IndexOfRefraction = 1.5f, Roughness = percent}, Radius) {Position = position.ZYX});
				children.Add(new SphereObject(new Glass {Albedo = ballWhite, IndexOfRefraction = Scalars.Lerp(1.1f, 2.1f, percent)}, Radius) {Position = position.XYW});
				children.Add(new SphereObject(new Glossy {Albedo = ballWhite, Smoothness = percent}, Radius) {Position = position.WYX});
			}

			Camera camera = new Camera(42f) {Position = new Float3(0f, Half, -Half)};

			float radian = camera.FieldOfView / 2f * Scalars.DegreeToRadian;
			camera.Position += Float3.backward * (Half / MathF.Tan(radian));

			children.Add(camera);
		}
	}

	public class Sponza : Scene
	{
		public Sponza()
		{
			var mesh = new Mesh("Assets/Models/CrytekSponza/sponza.obj");
			var materials = new MaterialLibrary("Assets/Models/CrytekSponza/sponza.mat");

			// materials["light"] = new Invisible();

			children.Add(new MeshObject(mesh, materials) {Rotation = Float3.up * 90f});

			Cubemap = new SolidCubemap(new Float3(10.3f, 8.9f, 6.3f));

			// children.Add(new Camera(90f) {Position = new Float3(-9.4f, 16.1f, -4.5f), Rotation = new Float3(13.8f, 43.6f, 0f)});
			children.Add(new Camera(90f) {Position = new Float3(2.8f, 7.5f, -1.7f), Rotation = new Float3(6.8f, -12.6f, 0f)});
		}
	}

	public class SingleDragonScene : Scene
	{
		public SingleDragonScene()
		{
			var mesh = new Mesh("Assets/Models/StanfordDragon/StanfordDragon.obj");
			var materials = new MaterialLibrary("Assets/Models/StanfordDragon/StanfordDragon.mat");

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea", (Float3)1.2f);

			children.Add(new PlaneObject(new Glossy {Albedo = new Float3(0.29f, 0.29f, 0.35f), Smoothness = 0.85f}, (Float2)24f));
			children.Add(new MeshObject(mesh, materials) {Position = Float3.zero, Rotation = new Float3(0f, 158f, 0f), Scale = (Float3)3.5f});

			children.Add(new Camera(100f) {Position = new Float3(0f, 4f, -8f), Rotation = new Float3(10f, 0f, 0f)});

			//Lights
			children.Add(new SphereObject(new Emissive {Emission = new Float3(5f, 5f, 6f)}, 17f) {Position = new Float3(23f, 34f, -18f)});  //Bottom right
			children.Add(new SphereObject(new Emissive {Emission = new Float3(5f, 4f, 5f)}, 19f) {Position = new Float3(-27f, 31f, -20f)}); //Bottom left

			children.Add(new SphereObject(new Emissive {Emission = new Float3(0.6f, 0.1f, 0.3f)}, 1f) {Position = new Float3(-7f, 1f, 4f)});
			children.Add(new SphereObject(new Emissive {Emission = new Float3(0.3f, 0.1f, 0.6f)}, 1f) {Position = new Float3(7f, 1f, 4f)});
		}
	}

	public class SingleBMWScene : StandardScene
	{
		public SingleBMWScene() : base(new Glossy {Albedo = (Float3)0.88f, Smoothness = 0.78f})
		{
			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			Material dark = new Glossy {Albedo = (Float3)0.3f, Smoothness = 0.9f};
			children.Add(new MeshObject(mesh, dark) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});
		}
	}

	public class LightedBMWScene : StandardScene
	{
		public LightedBMWScene()
		{
			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			var materials = new MaterialLibrary("Assets/Models/BlenderBMW/BlenderBMW.mat");

			Cubemap = new SolidCubemap((Float3)0.2f);

			children.Add(new MeshObject(mesh, materials) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});

			children.Add(new SphereObject(new Emissive {Emission = new Float3(7f, 4f, 8f)}, 8f) {Position = new Float3(24f, 15f, 18f)});   //Upper right purple
			children.Add(new SphereObject(new Emissive {Emission = new Float3(8f, 4f, 3f)}, 5f) {Position = new Float3(-16f, 19f, -12f)}); //Bottom left orange
			children.Add(new SphereObject(new Emissive {Emission = new Float3(2f, 7f, 4f)}, 7f) {Position = new Float3(10f, 24f, -12f)});  //Bottom right green
			children.Add(new SphereObject(new Emissive {Emission = new Float3(3f, 4f, 8f)}, 8f) {Position = new Float3(-19f, 19f, 13f)});  //Upper left blue
		}
	}

	public class MultipleBMWScene : StandardScene //Large scene, stress test (42 million triangles)
	{
		public MultipleBMWScene() : base(new Glossy {Albedo = (Float3)0.88f, Smoothness = 0.78f})
		{
			Int2 min = new Int2(-4, -1);
			Int2 max = new Int2(2, 2);

			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			Gradient gradientX = new Gradient {{0f, Float4.one}, {1f, Utilities.ToColor(0.2f)}};
			Gradient gradientY = new Gradient {{0f, Utilities.ToColor("DD444C")}, {1f, Utilities.ToColor("EEE")}};

			foreach (Int2 xz in new EnumerableSpace2D(min, max))
			{
				Float2 percent = Float2.InverseLerp(min, max, xz);

				Material material = new Glossy {Albedo = (gradientX[percent.x] * gradientY[percent.y]).XYZ, Smoothness = 0.85f};
				Float3 position = new Float3(2.8f, 0f, -0.8f) * xz.x + new Float3(1.7f, 0f, 0.2f + xz.y * 6.1f);

				children.Add(new MeshObject(mesh, material) {Position = position, Rotation = new Float3(0f, 120f, 0f)});
			}
		}
	}

	public class RandomSpheresScene : StandardScene
	{
		public RandomSpheresScene(int count)
		{
			AddSpheres(new MinMax(0f, 7f), new MinMax(0.4f, 0.7f), count);
			AddSpheres(new MinMax(0f, 7f), new MinMax(0.1f, 0.2f), count * 3);
		}

		void AddSpheres(MinMax positionRange, MinMax radiusRange, int count)
		{
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

				bool metal = RandomHelper.Value < 0.3d;
				bool emissive = RandomHelper.Value < 0.05d;

				Material material;

				if (metal) material = new Glossy {Albedo = color, Smoothness = (float)RandomHelper.Value / 2f + 0.5f};
				else if (emissive) material = new Emissive {Emission = color / color.MaxComponent * 3f};
				else material = new Diffuse {Albedo = color};

				children.Add(new SphereObject(material, radius) {Position = position});
			}
		}

		bool IntersectingOthers(float radius, Float3 position)
		{
			for (int i = 0; i < children.Count; i++)
			{
				var sphere = children[i] as SphereObject;
				if (sphere == null) continue;

				float distance = sphere.Radius + radius;
				if ((sphere.Position - position).SquaredMagnitude <= distance * distance) return true;
			}

			return false;
		}
	}

	public class GridSpheresScene : Scene
	{
		public GridSpheresScene()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
			children.Add(new Camera(100f) {Position = new Float3(0f, 0f, -3f), Rotation = Float3.zero});

			const int Width = 10;
			const int Height = 4;

			for (float i = 0f; i <= Width; i++)
			{
				for (float j = 0f; j <= Height; j++)
				{
					// var material = new Glossy {Albedo = Float3.Lerp((Float3)0.9f, new Float3(0.867f, 0.267f, 0.298f), j / Height), Smoothness = i / Width};
					var material = new Glass {Albedo = (Float3)0.9f, IndexOfRefraction = Scalars.Lerp(1.1f, 1.7f, j / Height), Roughness = i / Width};

					children.Add(new SphereObject(material, 0.45f) {Position = new Float3(i - Width / 2f, j - Height / 2f, 2f)});
				}
			}
		}
	}
}