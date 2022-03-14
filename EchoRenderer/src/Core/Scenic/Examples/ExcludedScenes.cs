using CodeHelpers.Mathematics;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Scenic.Lights;
using EchoRenderer.Core.Texturing;
using EchoRenderer.Core.Texturing.Directional;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.Scenic.Examples;

public class BallRoom : Scene
{
	public BallRoom()
	{
		children.Add(new AmbientLight { Texture = new Cubemap("Assets/Cubemaps/OutsideDayTime") { Tint = Tint.Scale((Float3)0.02f) } });

		var mesh = new Mesh("Assets/Models/Excluded/BallRoom/ballRoom.obj");
		var materials = new MaterialLibrary("Assets/Models/Excluded/BallRoom/ballRoom.mat");

		children.Add(new MeshEntity { Mesh = mesh, MaterialLibrary = materials });

		children.Add(new Camera(84f) { Position = new Float3(2.2f, 1.7f, -7.3f), Rotation = new Float3(-2f, 76f, 0f) });
	}
}