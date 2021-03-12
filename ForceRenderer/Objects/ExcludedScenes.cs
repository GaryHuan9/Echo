using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Textures;

namespace ForceRenderer.Objects
{
	public class InteriorScene : Scene
	{
		public InteriorScene()
		{
			Cubemap = new SolidCubemap(3f);

			var mesh = new Mesh("Assets/Models/Excluded/Decorator C4D Interior 004/interior.obj");
			var materials = new MaterialLibrary("Assets/Models/Excluded/Decorator C4D Interior 004/interior.mat");

			children.Add(new MeshObject(mesh, materials) {Rotation = Float3.up * 180f});
			children.Add(new Camera(120f) {Position = new Float3(-1f, 3f, 5f), Rotation = new Float3(3f, 74f, 0f)});
		}
	}
}