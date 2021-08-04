using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;

namespace EchoRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
	{
		public override MemoryArena CreateArena(int hash) => new MemoryArena(hash);

		public override Sample Render(Float2 screenUV, MemoryArena arena)
		{
			PressedScene scene = Profile.Scene;
			ExtendedRandom random = arena.random;

			Ray ray = scene.camera.GetRay(screenUV, random);

			Float3 energy = Float3.one;
			Float3 colors = Float3.zero;

			int bounce = 0;

			//Auxiliary data
			Float3 firstAlbedo = Float3.zero;
			Float3 firstNormal = Float3.up;
			float firstZDepth = float.PositiveInfinity;

			bool missingAuxiliary = true;

			while (bounce < Profile.BounceLimit && scene.GetIntersection(ray, out CalculatedHit hit))
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
					firstZDepth = hit.distance;

					missingAuxiliary = false;
				}

				colors += energy * emission;
				energy *= albedo;

				if (energy <= Profile.EnergyEpsilon) break;
				ray = CreateBiasedRay(direction, hit);
			}

#if DEBUG
			//The bounce limit is supposed to be significantly higher than the average bounce count
			if (bounce >= Profile.BounceLimit) CodeHelpers.Diagnostics.DebugHelper.Log("Bounce limit reached!");
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

			return new Sample(colors.Max(Float3.zero), firstAlbedo, firstNormal, firstZDepth);
		}
	}
}