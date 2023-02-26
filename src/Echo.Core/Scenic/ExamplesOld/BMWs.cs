namespace Echo.Core.Scenic.ExamplesOld
{
	public class SingleBMW : StandardScene
	{
		public SingleBMW() : base(new Glossy {Albedo = (Float3)0.88f, Smoothness = 0.78f})
		{
			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			MaterialOld dark = new Glossy {Albedo = (Float3)0.3f, Smoothness = 0.9f};
			children.Add(new MeshObject(mesh, dark) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});
		}
	}

	public class LightedBMW : StandardScene
	{
		public LightedBMW()
		{
			var mesh = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			var materials = new MaterialLibrary("Assets/Models/BlenderBMW/BlenderBMW.mat");

			Cubemap = new GradientCubemap(new Gradient {{0f, Utilities.ToColor(0.02f)}, {1f, Utilities.ToColor(0.07f)}});

			children.Add(new MeshObject(mesh, materials) {Position = Float3.zero, Rotation = new Float3(0f, 115f, 0f), Scale = (Float3)1.4f});

			children.Add(new SphereObject(new Emissive {Emission = new Float3(7f, 4f, 8f)}, 8f) {Position = new Float3(24f, 15f, 18f)});   //Upper right purple
			children.Add(new SphereObject(new Emissive {Emission = new Float3(8f, 4f, 3f)}, 5f) {Position = new Float3(-16f, 19f, -12f)}); //Bottom left orange
			children.Add(new SphereObject(new Emissive {Emission = new Float3(2f, 7f, 4f)}, 7f) {Position = new Float3(10f, 24f, -12f)});  //Bottom right green
			children.Add(new SphereObject(new Emissive {Emission = new Float3(3f, 4f, 8f)}, 8f) {Position = new Float3(-19f, 19f, 13f)});  //Upper left blue
		}
	}
}