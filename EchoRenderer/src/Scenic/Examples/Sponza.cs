using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Scenic.GeometryObjects;
using EchoRenderer.Scenic.Lights;
using EchoRenderer.Textures;

namespace EchoRenderer.Scenic.Examples
{
	public class Sponza : Scene
	{
		public Sponza()
		{
			var mesh = new Mesh("Assets/Models/CrytekSponza/sponza.obj");
			var materials = new MaterialLibrary("Assets/Models/CrytekSponza/sponza.mat");

			// materials["light"] = new Invisible();

			children.Add(new MeshObject(mesh, materials) { Rotation = Float3.up * 90f });

			children.Add(new AmbientLight { Texture = (Pure)new Float3(10.3f, 8.9f, 6.3f) });

			children.Add(new Camera(90f) { Position = new Float3(-9.4f, 16.1f, -4.5f), Rotation = new Float3(13.8f, 43.6f, 0f) });
			// children.Add(new Camera(90f) {Position = new Float3(2.8f, 7.5f, -1.7f), Rotation = new Float3(6.8f, -12.6f, 0f)});
		}
	}
}