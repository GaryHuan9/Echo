using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Scenic.Instancing;
using EchoRenderer.Core.Texturing;

namespace EchoRenderer.Core.Scenic;

public class Scene : EntityPack { }

public class StandardScene : Scene
{
	public StandardScene(Material ground = null)
	{
		// AddSkybox(new Cubemap("Assets/Cubemaps/OutsideSea"));

		children.Add(new PlaneEntity { Material = ground ?? new Matte { Albedo = (Pure)new RGBA32(0.75f) }, Size = new Float2(32f, 24f) });
		// children.Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f)});

		children.Add(new Camera(110f) { Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f) });
	}
}