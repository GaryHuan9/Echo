using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override Float3 Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			while (GetIntersection(ray, out Hit hit))
			{
				CalculatedHit calculated = new CalculatedHit(hit, ray, Profile.scene);
				Material material = Profile.scene.GetMaterial(hit);

				Float4 sample = material.AlbedoMap[calculated.texcoord];
				if (!Scalars.AlmostEquals(sample.w, 0f)) return sample.XYZ * material.Albedo;

				ray = new Ray(calculated.position, calculated.direction, true);
			}

			return scene.cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}