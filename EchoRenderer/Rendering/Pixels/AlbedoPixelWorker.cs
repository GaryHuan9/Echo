using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Rendering.Pixels
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
				ray = CreateBiasedRay(ray.direction, hit);
			}

			return scene.cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}