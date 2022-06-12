using CodeHelpers.Packed;
using Echo.Core.InOut;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Examples;

public class BallRoom : Scene
{
	public BallRoom()
	{
		children.Add(new AmbientLight { Texture = new Cubemap("Assets/Cubemaps/OutsideDayTime") { Tint = Tint.Scale(new RGBA128(0.02f)) } });

		var mesh = new Mesh("Assets/Models/Excluded/BallRoom/ballRoom.obj");
		var materials = new MaterialLibrary("Assets/Models/Excluded/BallRoom/ballRoom.mat");

		children.Add(new MeshEntity { Mesh = mesh, MaterialLibrary = materials });

		children.Add(new Camera(84f) { Position = new Float3(2.2f, 1.7f, -7.3f), Rotation = new Float3(-2f, 76f, 0f) });
	}
}