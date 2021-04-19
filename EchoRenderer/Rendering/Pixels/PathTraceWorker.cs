using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
	{
		public override Sample Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			ExtendedRandom random = Random;
			int bounce = 0;

			Float3 energy = Float3.one;
			Float3 colors = Float3.zero;

			//Auxiliary data
			Float3 firstAlbedo = Float3.zero;
			Float3 firstNormal = Float3.zero;
			bool missingAuxiliary = true;

			while (bounce < Profile.bounceLimit && scene.GetIntersection(ray, out CalculatedHit hit))
			{
				++bounce;

				Float3 emission = hit.material.Emit(hit, random);
				Float3 albedo = hit.material.BidirectionalScatter(hit, random, out Float3 direction);

				if (HitPassThrough(hit, albedo, direction))
				{
					ray = CreateBiasedRay(ray.direction, hit);
					continue;
				}

				if (missingAuxiliary)
				{
					firstAlbedo = albedo;
					firstNormal = hit.normal;

					missingAuxiliary = false;
				}

				if (direction == Float3.zero) albedo = Float3.zero;

				colors += energy * emission;
				energy *= albedo;

				if (energy <= Profile.energyEpsilon) break;
				ray = CreateBiasedRay(direction, hit);
			}

#if DEBUG
			//The bounce limit is supposed to be significantly higher than the average bounce count
			if (bounce >= Profile.bounceLimit) DebugHelper.Log("Bounce limit reached!");
#endif

			var cubemap = scene.cubemap;
			var lights = scene.lights;

			if (cubemap != null) colors += energy * cubemap.Sample(ray.direction);

			if (bounce > 0)
			{
				//Add light colors if we actually hit some geometry

				for (int i = 0; i < lights.Count; i++)
				{
					PressedLight light = lights[i];

					float weight = -light.direction.Dot(ray.direction);
					if (weight > light.threshold) colors += energy * light.intensity * weight;
				}
			}
			else firstAlbedo = colors; //No bounce sample do not have albedo so we just use the skybox

			return new Sample(colors.Max(Float3.zero), firstAlbedo, firstNormal);
		}
	}
}