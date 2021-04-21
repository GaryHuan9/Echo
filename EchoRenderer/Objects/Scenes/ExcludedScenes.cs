using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Textures.Cubemaps;

namespace EchoRenderer.Objects.Scenes
{
	public class BallRoom : Scene
	{
		public BallRoom()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)16f);

			var mesh = new Mesh("Assets/Models/Excluded/BallRoom/ballRoom.obj");
			var materials = new MaterialLibrary("Assets/Models/Excluded/BallRoom/ballRoom.mat");

			children.Add(new MeshObject(mesh, materials));

			children.Add(new Camera(90f) {Position = new Float3(3f, 2f, -10f), Rotation = new Float3(10f, 0f, 0f)});
		}
	}
}