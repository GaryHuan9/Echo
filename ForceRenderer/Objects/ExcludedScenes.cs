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
			Cubemap = new SolidCubemap(1f);

			var mesh = new Mesh("Assets/Models/Excluded/Decorator C4D Interior 004/interior.obj");
			var materials = new MaterialLibrary("Assets/Models/Excluded/Decorator C4D Interior 004/interior.mat");

			children.Add(new MeshObject(mesh, materials) {Rotation = Float3.up * 180f});
			children.Add(new Camera(120f) {Position = new Float3(2f, 3f, 7f), Rotation = new Float3(5f, 42f, 0f)});
		}
	}
}