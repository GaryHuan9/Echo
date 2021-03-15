using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Textures;

namespace ForceRenderer.Objects
{
	public class BugattiScene : Scene
	{
		public BugattiScene()
		{
			Cubemap = new SolidCubemap(0.35f);

			var mesh = new Mesh("Assets/Models/Excluded/Bugatti/bugatti.obj");
			var materials = new MaterialLibrary("Assets/Models/Excluded/Bugatti/bugatti.mat");

			children.Add(new MeshObject(mesh, materials) {Rotation = Float3.up * 180f});
			children.Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ * 1.5f, Rotation = new Float3(60f, 30f, 0f)});

			Camera camera = new Camera(85f) {Position = new Float3(6.5f, 4.5f, -7.5f)};

			camera.LookAt(Float3.up);
			children.Add(camera);
		}
	}
}