using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Objects;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Rendering.Materials;
using ForceRenderer.Textures;

namespace ForceRenderer
{
	public class StandardScene : Scene
	{
		public StandardScene(Material ground = null)
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");

			children.Add(new PlaneObject(ground ?? new Diffuse {Albedo = (Float3)0.75f}, new Float2(24f, 16f)));
			children.Add(new Camera(110f) {Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f)});
		}
	}

	public class TestLighting : StandardScene
	{
		public TestLighting()
		{
			Cubemap = null;

			children.Add(new DirectionalLight {Intensity = Float3.one * 2f, Rotation = new Float3(45f, 45f, 0f)});
			// children.Add(new PlaneObject(new Material {Emission = Float3.one * 5f}, new Float2(10f, 10f)) {Position = Float3.forward * 5f, Rotation = Float3.right * -90f});
			children.Add(new BoxObject(new Diffuse {Albedo = Float3.one}, Float3.one * 2f) {Position = Float3.up});
		}
	}

	public class TestTexture : StandardScene
	{
		public TestTexture()
		{
			Texture2D texture = Texture2D.Load("Assets/Textures/MinecraftTexture.bmp");
			Material material = new Diffuse {Albedo = Float3.one, AlbedoMap = texture};

			Cubemap = new SolidCubemap(Color32.white);
			children.Add(new PlaneObject(material, Float2.one * 18f) {Position = new Float3(-9f, 2.5f, -5f), Rotation = new Float3(-90f, 0f, 0f)});
		}
	}

	public class TestTransparency : Scene
	{
		public TestTransparency()
		{
			// children.Add(new SphereObject(new Material {Diffuse = new Float3(0.8f, 0.8f, 0f)}, 100f) {Position = new Float3(0f, -100.5f, 0f)});

			Material material = new Glass {IndexOfRefraction = 1.5f, Transmission = (Float3)0.9f};
			children.Add(new BoxObject(material, new Float3(4f, 1f, 0.03f)) {Position = new Float3(0f, 0.5f, 0f)});

			children.Add(new SphereObject(new Glass {IndexOfRefraction = 1.5f, Transmission = Float3.one}, 0.5f) {Position = new Float3(-1f, 0f, 0f)});
			children.Add(new SphereObject(new Diffuse {Albedo = new Float3(0.8f, 0.6f, 0.2f)}, 0.5f) {Position = new Float3(1f, 0f, 0f)});

			var camera = new Camera(90f) {Position = new Float3(0.2f, 2f, -1f)};

			camera.LookAt(Float3.zero);
			children.Add(camera);

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
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

	public class KunaiScene : Scene
	{
		public KunaiScene()
		{
			var kunai = new Mesh("Assets/Models/Kunai/wraith_kunai.obj");
			var materials = new MaterialLibrary("Assets/Models/Kunai/wraith_kunai.mat");

			Cubemap = new SolidCubemap(Color32.white);

			var target = new MeshObject(kunai, materials) {Rotation = new Float3(0f, 0f, -65f)};
			var camera = new Camera(80f) {Position = new Float3(-0.2f, 0.55f, 0.7f)};

			children.Add(target);
			children.Add(camera);

			camera.LookAt(target);
		}
	}

	public class CornellBox : Scene
	{
		public CornellBox()
		{
			var mesh = new Mesh("Assets/Models/CornellBox/CornellBox.obj");
			var materials = new MaterialLibrary("Assets/Models/CornellBox/CornellBox.mat");

			children.Add(new MeshObject(mesh, materials));
			children.Add(new Camera(90f) {Position = new Float3(0f, 1f, 2.2f), Rotation = Float3.up * 180f});
		}
	}

	public class SingleBMWScene : StandardScene
	{
		public SingleBMWScene() : base(new Metal {Albedo = (Float3)0.88f, Smoothness = 0.78f})
		{
			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			Material dark = new Metal {Albedo = (Float3)0.3f, Smoothness = 0.9f};
			children.Add(new MeshObject(mesh, dark) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});
		}
	}

	public class LightedBMWScene : StandardScene
	{
		public LightedBMWScene()
		{
			// var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			// var materials = new MaterialLibrary("Assets/Models/BlenderBMW/BlenderBMW.mat");

			Cubemap = new SolidCubemap((Color32)(Float3)0.21f);

			// children.Add(new MeshObject(mesh, materials) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});

			children.Add(new SphereObject(new Emissive {Emission = new Float3(7f, 4f, 8f)}, 8f) {Position = new Float3(24f, 15f, 18f)});   //Upper right purple
			children.Add(new SphereObject(new Emissive {Emission = new Float3(8f, 4f, 3f)}, 5f) {Position = new Float3(-16f, 19f, -12f)}); //Bottom left orange
			children.Add(new SphereObject(new Emissive {Emission = new Float3(2f, 7f, 4f)}, 7f) {Position = new Float3(10f, 24f, -12f)});  //Bottom right green
			children.Add(new SphereObject(new Emissive {Emission = new Float3(3f, 4f, 8f)}, 8f) {Position = new Float3(-19f, 19f, 13f)});  //Upper left blue
		}
	}

	public class MultipleBMWScene : StandardScene
	{
		public MultipleBMWScene() : base(new Metal {Albedo = (Float3)0.88f, Smoothness = 0.78f})
		{
			MinMaxInt range = new MinMaxInt(-3, 1);

			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			foreach (int index in range.Loop())
			{
				Material material = new Metal {Albedo = (Float3)range.InverseLerp(index), Smoothness = 0.85f};
				Float3 position = new Float3(2.8f, 0f, -0.8f) * index + new Float3(1.7f, 0f, 0.2f);

				children.Add(new MeshObject(mesh, material) {Position = position, Rotation = new Float3(0f, 120f, 0f)});
			}
		}
	}

	public class SingleDragonScene : Scene
	{
		public SingleDragonScene()
		{
			var mesh = new Mesh("Assets/Models/StanfordDragon/StanfordDragon.obj");
			var materials = new MaterialLibrary("Assets/Models/StanfordDragon/StanfordDragon.mat");

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");

			children.Add(new PlaneObject(new Metal {Albedo = new Float3(0.29f, 0.29f, 0.35f), Smoothness = 0.85f}, (Float2)24f));
			children.Add(new MeshObject(mesh, materials) {Position = Float3.zero, Rotation = new Float3(0f, 158f, 0f), Scale = (Float3)3.5f});

			children.Add(new Camera(100f) {Position = new Float3(0f, 4f, -8f), Rotation = new Float3(10f, 0f, 0f)});

			//Lights
			children.Add(new SphereObject(new Emissive {Emission = new Float3(6f, 6f, 7f)}, 17f) {Position = new Float3(23f, 34f, -18f)});  //Bottom right
			children.Add(new SphereObject(new Emissive {Emission = new Float3(6f, 5f, 6f)}, 19f) {Position = new Float3(-27f, 31f, -20f)}); //Bottom left

			children.Add(new SphereObject(new Emissive {Emission = new Float3(0.6f, 0.1f, 0.3f)}, 1f) {Position = new Float3(-7f, 1f, 4f)});
			children.Add(new SphereObject(new Emissive {Emission = new Float3(0.3f, 0.1f, 0.6f)}, 1f) {Position = new Float3(7f, 1f, 4f)});
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

				if (metal) material = new Metal {Albedo = color, Smoothness = (float)RandomHelper.Value / 2f + 0.5f};
				else if (emissive) material = new Emissive {Emission = color * 10f};
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
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");
			children.Add(new Camera(100f) {Position = new Float3(0f, 0f, -3f), Rotation = Float3.zero});

			const int SmoothnessLevel = 10;
			const int SpecularLevel = 4;

			for (float i = 0f; i <= SmoothnessLevel; i++)
			{
				for (float j = 0f; j <= SpecularLevel; j++)
				{
					var metal = new Metal {Albedo = Float3.Lerp((Float3)0.9f, new Float3(0.867f, 0.267f, 0.298f), j / SpecularLevel), Smoothness = i / SmoothnessLevel};
					children.Add(new SphereObject(metal, 0.45f) {Position = new Float3(i - SmoothnessLevel / 2f, j - SpecularLevel / 2f, 2f)});
				}
			}
		}
	}

	public class TestNewMaterialScene : Scene
	{
		public TestNewMaterialScene()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");

			children.Add(new Camera(90f) {Position = new Float3(0f, 1f, -2f)});

			children.Add(new SphereObject(new Diffuse {Albedo = (Float3)0.9f}, 1f) {Position = new Float3(0f, 1f, 2f)});
			// children.Add(new SphereObject(new Metal {Albedo = (Float3)0.7f, Smoothness = 0.5f}, 1f) {Position = new Float3(2f, 1f, 2f)});
			children.Add(new SphereObject(new Glass {Transmission = (Float3)0.9f, IndexOfRefraction = 1.5f, Roughness = 0.5f}, 1f) {Position = new Float3(2f, 1f, 2f)});
			children.Add(new SphereObject(new Glass {Transmission = (Float3)0.9f, IndexOfRefraction = 1.5f}, 1f) {Position = new Float3(-2f, 1f, 2f)});

			children.Add(new PlaneObject(new Diffuse {Albedo = (Float3)0.9f}, new Float2(24f, 16f)) {Position = new Float3(0f, 0f, 0f)});
		}
	}
}