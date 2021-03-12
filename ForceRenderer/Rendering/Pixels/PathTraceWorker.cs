using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Rendering.Pixels
{
	public class PathTraceWorker : PixelWorker
	{
		public override Float3 Render(Float2 screenUV)
		{
			PressedScene scene = Profile.scene;
			Ray ray = scene.camera.GetRay(screenUV);

			Float3 energy = Float3.one; //AKA energy
			Float3 color = Float3.zero;

			ExtendedRandom random = Random;
			int bounce = 0;

			for (; bounce < Profile.bounceLimit; bounce++)
			{
				if (!GetIntersection(ray, out Hit hit)) break;

				CalculatedHit calculated = new CalculatedHit(hit, ray, scene);
				Material material = scene.GetMaterial(hit);

				material.ApplyNormal(calculated);

				Float3 emission = material.Emit(calculated, random);
				Float3 bsdf = material.BidirectionalScatter(calculated, random, out Float3 direction);

				color += energy * emission;
				energy *= bsdf;

				if (energy <= Profile.energyEpsilon) break;
				ray = new Ray(calculated.position, direction, true);
			}

			var cubemap = scene.cubemap;
			var lights = scene.lights;

			if (bounce == 0) return color + cubemap?.Sample(ray.direction) ?? Float3.zero;
			if (cubemap != null) color += energy * cubemap.Sample(ray.direction);

			for (int i = 0; i < lights.Count; i++)
			{
				PressedLight light = lights[i];

				float weight = -light.direction.Dot(ray.direction);
				if (weight > light.threshold) color += energy * light.intensity * weight;
			}

			return color.Max(Float3.zero); //Do not clamp up, because emissive samples can go beyond 1f
		}
	}

	public readonly struct CalculatedHit
	{
		public CalculatedHit(in Hit hit, in Ray ray, PressedScene scene)
		{
			position = ray.GetPoint(hit.distance);
			direction = ray.direction;

			normal = scene.GetNormal(hit);
			texcoord = scene.GetTexcoord(hit);
		}

		public readonly Float3 position;
		public readonly Float3 direction;

		public readonly Float3 normal;
		public readonly Float2 texcoord;

		// public readonly float distance;
	}
}