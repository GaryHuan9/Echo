using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
	{
		public override Float3 Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			Float3 energy = Float3.one;
			Float3 colors = Float3.zero;

			ExtendedRandom random = Random;
			int bounce = 0;

			while (bounce < Profile.bounceLimit && scene.GetIntersection(ray, out CalculatedHit hit))
			{
				++bounce;

				Float3 emission = hit.material.Emit(hit, random);
				Float3 bsdf = hit.material.BidirectionalScatter(hit, random, out Float3 direction);

				colors += energy * emission;
				energy *= bsdf;

				if (energy <= Profile.energyEpsilon) break;
				ray = CreateBiasedRay(direction, hit);
			}

#if DEBUG
			//The bounce limit is supposed to be significantly higher than the average bounce count
			if (bounce >= Profile.bounceLimit) DebugHelper.Log("Bounce limit reached!");
#endif

			var cubemap = scene.cubemap;
			var lights = scene.lights;

			if (bounce == 0) return colors + cubemap?.Sample(ray.direction) ?? Float3.zero;
			if (cubemap != null) colors += energy * cubemap.Sample(ray.direction);

			for (int i = 0; i < lights.Count; i++)
			{
				PressedLight light = lights[i];

				float weight = -light.direction.Dot(ray.direction);
				if (weight > light.threshold) colors += energy * light.intensity * weight;
			}

			return colors.Max(Float3.zero); //Do not clamp up, because emissive samples can go beyond 1f
		}
	}
}