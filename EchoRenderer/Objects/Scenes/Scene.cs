using System.Collections.Generic;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Textures;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Objects.Scenes
{
	public class Scene : ObjectPack
	{
		public EnvironmentalLight AddSkybox(IDirectionalTexture texture)
		{
			var light = new EnvironmentalLight { Texture = texture };

			children.Add(light);
			return light;
		}
	}

	public class StandardScene : Scene
	{
		public StandardScene(Material ground = null)
		{
			AddSkybox(new Cubemap("Assets/Cubemaps/OutsideSea"));

			children.Add(new PlaneObject(ground ?? new Matte { Albedo = new Pure(0.75f) }, new Float2(32f, 24f)));
			// children.Add(new Light {Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f)});

			children.Add(new Camera(110f) { Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f) });
		}
	}
}