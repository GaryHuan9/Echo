using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Rendering.Materials;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering
{
	public class PathTraceWorker : PixelWorker
	{
		public PathTraceWorker(RenderEngine.Profile profile) : base(profile) { }

		public override Float3 Render(Float2 screenUV)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(screenUV));

			Float3 waves = Float3.one; //AKA energy
			Float3 color = Float3.zero;

			ExtendedRandom random = Random;
			int bounce = 0;

			for (; bounce < profile.maxBounce; bounce++)
			{
				if (!GetIntersection(ray, out Hit hit)) break;

				CalculatedHit calculated = new CalculatedHit(hit, ray, profile.pressed);
				Material material = profile.pressed.GetMaterial(hit);

				material.ApplyNormal(calculated);

				Float3 emission = material.Emit(calculated, random);
				Float3 bsdf = material.BidirectionalScatter(calculated, random, out Float3 direction);

				color += waves * emission;
				waves *= bsdf;

				if (waves <= profile.energyEpsilon) break;
				ray = new Ray(calculated.position, direction, true);
			}

			var cubemap = profile.scene.Cubemap;
			var lights = profile.pressed.lights;

			if (bounce == 0) return color + cubemap?.Sample(ray.direction) ?? Float3.zero;
			if (cubemap != null) color += waves * cubemap.Sample(ray.direction);

			for (int i = 0; i < lights.Count; i++)
			{
				PressedLight light = lights[i];

				float weight = -light.direction.Dot(ray.direction);
				if (weight > light.threshold) color += waves * light.intensity * weight;
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

			distance = hit.distance;
		}

		public readonly Float3 position;
		public readonly Float3 direction;

		public readonly Float3 normal;
		public readonly Float2 texcoord;

		public readonly float distance;
	}
}