using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using EchoRenderer.IO;
using EchoRenderer.Mathematics;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Textures;
using EchoRenderer.Textures.Cubemaps;

namespace EchoRenderer.Objects.Scenes
{
	public class TestScene : Scene
	{
		public TestScene()
		{
			children.Add(new SphereObject(new Diffuse {Albedo = Float3.one}, 1f));
			children.Add(new Camera(90f) {Position = new Float3(0f, 0f, -3f)});
		}
	}

	public class TestLighting : StandardScene
	{
		public TestLighting()
		{
			children.Add(new SphereObject(new Glossy {Albedo = Float3.right, Smoothness = 1f}, 0.5f) {Position = new Float3(2f, 0.5f, -2f)});
			children.Add(new SphereObject(new Diffuse {Albedo = Float3.forward}, 0.5f) {Position = new Float3(-1f, 0.5f, 1f)});
			children.Add(new BoxObject(new Glass {Albedo = Float3.up, IndexOfRefraction = 1.52f}, Float3.one) {Position = new Float3(-2f, 0.5f, -2f)});
			children.Add(new BoxObject(new Diffuse {Albedo = Float3.one * 0.9f}, Float3.one) {Position = new Float3(1f, 0.5f, 1f)});

			children.FindFirst<Camera>().FieldOfView = 80f;

			Light light = children.FindFirst<Light>();
			light.Coverage = 0.04f; //This makes the scene super noisy even after good number of samples
			light.Intensity *= 5f;  //We need a better way to trace global/directional lights

			Material core = new Diffuse {Albedo = Utilities.ToColor("DD444C").XYZ};
			Material solid = new Glossy {Albedo = (Float3)0.9f, Smoothness = 0.9f};

			var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			var materials = new MaterialLibrary(core) {["Core"] = core, ["Solid"] = solid};

			children.Add(new MeshObject(mesh, materials) {Position = new Float3(0f, 0f, -2.5f), Rotation = new Float3(0f, -90f, 0f)});
		}
	}

	public class TestTexture : StandardScene
	{
		public TestTexture()
		{
			Texture2D normal = Texture2D.Load("Assets/Textures/WikiNormalMap.png");
			Texture2D texture = Texture2D.Load("Assets/Textures/MinecraftTexture.bmp");
			Texture2D chain = Texture2D.Load("Assets/Textures/SponzaChain.png");

			Material material0 = new Glossy {Albedo = Float3.one, Smoothness = 1f};
			Material material1 = new Diffuse {Albedo = Float3.one, AlbedoMap = texture};
			Material material2 = new Diffuse {Albedo = Float3.one, AlbedoMap = chain};

			children.Add(new PlaneObject(material2, Float2.one * 4f) {Position = new Float3(0f, 2f, -2f), Rotation = new Float3(-90f, 0f, 0f)});
			children.Add(new PlaneObject(material1, Float2.one * 4f) {Position = new Float3(2f, 2f, -3.5f), Rotation = new Float3(-90f, 0f, 0f)});
			children.Add(new PlaneObject(material0, Float2.one * 4f) {Position = new Float3(0f, 2f, -5.3f), Rotation = new Float3(-90f, 0f, 0f)});
		}
	}

	public class TestTransparency : Scene
	{
		public TestTransparency()
		{
			// children.Add(new SphereObject(new Material {Diffuse = new Float3(0.8f, 0.8f, 0f)}, 100f) {Position = new Float3(0f, -100.5f, 0f)});

			Material material = new Glass {IndexOfRefraction = 1.5f, Albedo = (Float3)0.9f};
			children.Add(new BoxObject(material, new Float3(4f, 1f, 0.03f)) {Position = new Float3(0f, 0.5f, 0f)});

			children.Add(new SphereObject(new Glass {IndexOfRefraction = 1.5f, Albedo = Float3.one}, 0.5f) {Position = new Float3(-1f, 0f, 0f)});
			children.Add(new SphereObject(new Diffuse {Albedo = new Float3(0.8f, 0.6f, 0.2f)}, 0.5f) {Position = new Float3(1f, 0f, 0f)});

			var camera = new Camera(90f) {Position = new Float3(0.2f, 2f, -1f)};

			camera.LookAt(Float3.zero);
			children.Add(camera);

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
		}
	}

	public class TestLightBleed : Scene
	{
		public TestLightBleed()
		{
			Diffuse diffuse = new Diffuse {Albedo = (Float3)0.6f};
			Emissive light = new Emissive {Emission = (Float3)6000f};

			const float Size = 1f; //Try different scales to test for floating point errors (1 -> 2000)

			children.Add(new PlaneObject(diffuse, (Float2)4f * Size));
			children.Add(new PlaneObject(diffuse, (Float2)4f * Size) {Position = new Float3(0f, 2f, 2f) * Size, Rotation = new Float3(-90f, 0f, 0f)});

			children.Add(new PlaneObject(light, (Float2)3f * Size) {Position = new Float3(0f, -1f, 3f) * Size, Rotation = new Float3(-45f, 0f, 0f)});

			children.Add(new Camera(60f) {Position = new Float3(0f, 1f, 1f) * Size, Rotation = new Float3(40f, 0f, 0f)});
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
		}
	}

	public class TestMaterials : Scene
	{
		public TestMaterials()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)1.2f);

			children.Add(new Camera(90f) {Position = new Float3(0f, 3f, -5f), Rotation = new Float3(15f, 0f, 0f)});
			TestGenerative simplex = new TestGenerative(42, 4) {Tiling = (Float2)1f, Offset = (Float2)1f};

			Material floor = new Glossy {Albedo = Float3.one, AlbedoMap = simplex, Smoothness = 0.5f};
			Material glass = new Glass {Albedo = Float3.one, IndexOfRefraction = 1.5f, Roughness = 0.6f};

			children.Add(new PlaneObject(floor, new Float2(24f, 16f)));

			children.Add
			(
				new BoxObject(glass, new Float3(5f, 0.05f, 2f))
				{
					Position = new Float3(0f, 2.3f, 1f),
					Rotation = new Float3(-27f, 0f, 0f)
				}
			);

			children.Add(new SphereObject(new Diffuse {Albedo = (Float3)0.9f}, 1f) {Position = new Float3(0f, 1f, 2f)});
			children.Add(new SphereObject(new Glossy {Albedo = (Float3)0.9f, Smoothness = 0.8f}, 1f) {Position = new Float3(2f, 1f, 2f)});
			children.Add(new SphereObject(new Glass {Albedo = (Float3)0.9f, IndexOfRefraction = 1.5f}, 1f) {Position = new Float3(-2f, 1f, 2f)});
		}
	}

	public class TestInstancing : Scene
	{
		public TestInstancing()
		{
			var mesh = new Mesh("Assets/Models/StanfordBunny/bunny.obj");
			var materials = new MaterialLibrary("Assets/Models/StanfordBunny/bunny.mat");
			var material = new Diffuse {Albedo = Utilities.ToColor("DEADBEEF").XYZ};

			// var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			// var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

			ObjectPack bunny = new ObjectPack();
			ObjectPack bunnyWall = new ObjectPack();

			bunny.children.Add(new MeshObject(mesh, material) {Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)0.7f});
			bunny.children.Add(new SphereObject(materials.first, 0.1f) {Position = new Float3(-0.3f, 0.2f, -0.3f)});

			foreach (Int2 offset in new EnumerableSpace2D(new Int2(-8, -5), new Int2(8, 5)))
			{
				bunnyWall.children.Add(new ObjectPackInstance(bunny) {Position = offset.XY_});
			}

			for (int z = 0; z < 4; z++)
			{
				children.Add(new ObjectPackInstance(bunnyWall) {Position = new Float3(0f, 0f, z * 6f), Rotation = new Float3(0f, -20f * (z + 1f), 0f), Scale = (Float3)(z + 1f)});
			}

			bunnyWall.children.Add(new PlaneObject(materials.first, Float2.one) {Position = new Float3(1f, -1f, 0f), Rotation = new Float3(-90f, -10f, 0f)});
			// bunnyWall.children.Add(new ObjectPackInstance(bunnyWall)); //Tests recursive instancing

			children.Add(new BoxObject(materials.first, Float3.one));
			children.Add(new PlaneObject(material, Float2.one * 0.9f) {Position = new Float3(-1.1f, -0.4f, 0.3f), Rotation = new Float3(-70f, 20f, 30f)});

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)1.5f);

			var camera = new Camera(110f) {Position = new Float3(4f, 27f, -25f)};

			camera.LookAt(Float3.zero);
			children.Add(camera);
		}
	}
}