using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
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

			Float3 energy = Float3.one;
			Float3 light = Float3.zero;

			ExtendedRandom random = Random;

			for (int bounce = 0; bounce < profile.maxBounce; bounce++)
			{
				if (!GetIntersection(ray, out Hit hit)) break;

				CalculatedHit calculated = new CalculatedHit(hit, ray, profile.pressed);
				Material material = profile.pressed.GetMaterial(hit);

				material.ApplyNormal(calculated);

				Float3 emission = material.Emit(calculated, random);
				Float3 bsdf = material.BidirectionalScatter(calculated, random, out Float3 direction);

				light += energy * emission;
				energy *= bsdf;

				if (energy <= profile.energyEpsilon) break;
				ray = new Ray(calculated.position, direction, true);
			}

			Cubemap skybox = profile.scene.Cubemap;
			if (skybox != null) light += energy * skybox.Sample(ray.direction);

			return light.Max(Float3.zero); //Do not clamp up, because emissive samples can go beyond 1f
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