using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public class AlbedoWorker : PixelWorker
	{
		public AlbedoWorker(RenderEngine.Profile profile) : base(profile) { }

		public override Float3 Render(Float2 screenUV)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(screenUV));

			if (TryTrace(ray, out float distance, out int token, out Float2 uv))
			{
				ref PressedMaterial material = ref profile.pressed.GetMaterial(token);
				return material.albedo;
			}

			Cubemap skybox = profile.scene.Cubemap; //Sample skybox if present
			return skybox == null ? Float3.zero : (Float3)skybox.Sample(ray.direction);
		}
	}
}