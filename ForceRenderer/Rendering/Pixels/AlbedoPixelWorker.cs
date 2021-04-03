using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics.Intersections;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override Float3 Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			while (scene.GetIntersection(ray, out CalculatedHit hit))
			{
				Material material = hit.material;
				Float4 sample = material.AlbedoMap[hit.texcoord];

				if (!Scalars.AlmostEquals(sample.w, 0f)) return sample.XYZ * material.Albedo;
				ray = new Ray(hit.position, hit.direction, true);
			}

			return scene.cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}