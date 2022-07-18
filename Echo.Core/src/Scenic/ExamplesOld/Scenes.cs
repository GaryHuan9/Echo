using CodeHelpers.Mathematics.Enumerable;
using CodeHelpers.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.InOut;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Examples;

public class TestInstancing : Scene
{
	public TestInstancing()
	{
		var mesh = new Mesh("Assets/Models/StanfordBunny/lowPoly.obj");
		var material0 = new Matte { Albedo = Pure.white };
		var material1 = new Matte { Albedo = Pure.normal };

		// var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
		// var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

		EntityPack bunny = new EntityPack();
		EntityPack bunnyWall = new EntityPack();

		bunny.Add(new MeshEntity { Mesh = mesh, Material = material0, Rotation = new Float3(0f, 180f, 0f), Scale = 0.7f });
		bunny.Add(new SphereEntity { Material = material1, Radius = 0.1f, Position = new Float3(-0.3f, 0.2f, -0.3f) });

		foreach (Int2 offset in new EnumerableSpace2D(new Int2(-8, -5), new Int2(8, 5)))
		{
			bunnyWall.Add(new PackInstance { Pack = bunny, Position = offset.XY_ });
		}

		for (int z = 0; z < 4; z++)
		{
			Add(new PackInstance { Pack = bunnyWall, Position = new Float3(0f, 0f, z * 6f), Rotation = new Float3(0f, -20f * (z + 1f), 0f), Scale = z + 1f });
		}

		bunnyWall.Add(new PlaneEntity { Material = material0, Position = new Float3(1f, -1f, 0f), Rotation = new Float3(-90f, -10f, 0f) });
		// bunnyWall.Add(new PackInstance{EntityPack = bunnyWall}); //Tests recursive instancing

		Add(new BoxEntity { Material = material0, Size = Float3.One });
		Add(new PlaneEntity { Material = material1, Size = Float2.One * 0.9f, Position = new Float3(-1.1f, -0.4f, 0.3f), Rotation = new Float3(-70f, 20f, 30f) });

		Add(new AmbientLight { Texture = new Cubemap("Assets/Cubemaps/OutsideDayTime") { Tint = Tint.Scale(new RGBA128(1.5f)) } });

		var camera = new Camera(110f) { Position = new Float3(4f, 27f, -25f) };

		camera.LookAt(Float3.Zero);
		Add(camera);
	}
}