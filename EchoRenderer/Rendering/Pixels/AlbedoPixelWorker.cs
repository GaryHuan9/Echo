using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;

namespace EchoRenderer.Rendering.Pixels
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public override Arena CreateArena(int hash) => new Arena(hash);

		public override Sample Render(Float2 screenUV, Arena arena)
		{
			PressedScene scene = Profile.Scene;
			ExtendedRandom random = arena.random;

			HitQuery query = scene.camera.GetRay(screenUV, random);

			while (scene.GetIntersection(ref query))
			{
				Float3 albedo = query.shading.material.BidirectionalScatter(query, random, out Float3 direction);
				if (!HitPassThrough(query, albedo, direction)) return albedo; //Return intersected albedo color

				query.Next(query.ray.direction);
			}

			return scene.cubemap?.Sample(query.ray.direction) ?? Float3.zero;
		}
	}
}