using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override MemoryArena CreateArena(int hash) => new MemoryArena(hash);

		public override Sample Render(Float2 screenUV, MemoryArena arena)
		{
			PressedScene scene = Profile.Scene;
			ExtendedRandom random = arena.random;

			Ray ray = scene.camera.GetRay(screenUV, random);

			while (scene.GetIntersection(ray, out CalculatedHit hit))
			{
				Float3 albedo = hit.material.BidirectionalScatter(hit, random, out Float3 direction);

				if (HitPassThrough(hit, albedo, direction)) ray = CreateBiasedRay(ray.direction, hit);
				else return new Sample(albedo, albedo, hit.normal); //Return intersected albedo color
			}

			return scene.cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}