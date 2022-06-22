using CodeHelpers.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic;

public class Scene : EntityPack { }

public class StandardScene : Scene
{
	public StandardScene(Material ground = null)
	{
		// AddSkybox(new Cubemap("Assets/Cubemaps/OutsideSea"));

		Add(new PlaneEntity { Material = ground ?? new Matte { Albedo = (Pure)new RGBA128(0.75f) }, Size = new Float2(32f, 24f) });
		// Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f)});

		Add(new Camera(110f) { Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f) });
	}
}