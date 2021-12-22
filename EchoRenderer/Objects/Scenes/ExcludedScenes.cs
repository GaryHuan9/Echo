using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Objects.Scenes
{
	public class BallRoom : Scene
	{
		public BallRoom()
		{
			Skybox = new Cubemap("Assets/Cubemaps/OutsideDayTime", (Float3)0.02f);

			var mesh = new Mesh("Assets/Models/Excluded/BallRoom/ballRoom.obj");
			var materials = new MaterialLibrary("Assets/Models/Excluded/BallRoom/ballRoom.mat");

			children.Add(new MeshObject(mesh, materials));

			children.Add(new Camera(84f) {Position = new Float3(2.2f, 1.7f, -7.3f), Rotation = new Float3(-2f, 76f, 0f)});
		}
	}
}