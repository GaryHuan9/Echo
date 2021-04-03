using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.GeometryObjects;
using ForceRenderer.Rendering.Materials;
using ForceRenderer.Textures;

namespace ForceRenderer.Objects.Scenes
{
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
			Texture2D texture = Texture2D.Load("Assets/Textures/MinecraftTexture.bmp");
			Texture2D normal = Texture2D.Load("Assets/Textures/WikiNormalMap.png");
			Texture2D chain = Texture2D.Load("Assets/Textures/SponzaChain.png");

			// Material material = new Diffuse {Albedo = Float3.one, AlbedoMap = texture};
			// Material material = new Diffuse {Albedo = Float3.one, NormalMap = normal};
			Material material = new Diffuse {Albedo = Float3.one, AlbedoMap = chain};

			Cubemap = new SolidCubemap(Float3.one);

			// children.Add(new PlaneObject(material, Float2.one * 4f) {Position = new Float3(0f, 2f, -5.5f), Rotation = new Float3(-90f, 0f, 0f)});
			children.Add(new PlaneObject(material, Float2.one * 4f) {Position = new Float3(0f, 2f, -2f), Rotation = new Float3(-90f, 0f, 0f)});
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

	public class TestMaterials
	{

	}
}