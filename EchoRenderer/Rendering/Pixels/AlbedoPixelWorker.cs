using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override Sample Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			ExtendedRandom random = Random;

			while (scene.GetIntersection(ray, out CalculatedHit hit))
			{
				Float3 albedo = hit.material.BidirectionalScatter(hit, random, out Float3 direction);
				if (HitPassThrough(hit, albedo, direction)) return new Sample(albedo, albedo, hit.normal);

				//Continue forward if material did not alter direction
				ray = CreateBiasedRay(ray.direction, hit);
			}

			return scene.cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}