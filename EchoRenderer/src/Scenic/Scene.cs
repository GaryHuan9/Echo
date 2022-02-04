using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Scenic.Geometries;
using EchoRenderer.Scenic.Instancing;
using EchoRenderer.Textures;

namespace EchoRenderer.Scenic;

public class Scene : EntityPack { }

public class StandardScene : Scene
{
	public StandardScene(Material ground = null)
	{
		// AddSkybox(new Cubemap("Assets/Cubemaps/OutsideSea"));

		children.Add(new PlaneEntity(ground ?? new Matte { Albedo = (Pure)0.75f }, new Float2(32f, 24f)));
		// children.Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f)});

		children.Add(new Camera(110f) { Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f) });
	}
}