using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public class PathTraceWorkerNew : PixelWorker
	{
		public PathTraceWorkerNew(RenderEngine.Profile profile) : base(profile) { }

		/// <summary>
		/// Returns a random vector inside a unit sphere.
		/// </summary>
		Float3 RandomInSphere
		{
			get
			{
				Float3 random;

				do random = new Float3(RandomValue * 2f - 1f, RandomValue * 2f - 1f, RandomValue * 2f - 1f);
				while (random.SquaredMagnitude > 1f);

				return random;
			}
		}

		/// <summary>
		/// Returns a random unit vector that is on a unit sphere.
		/// </summary>
		Float3 RandomOnSphere => RandomInSphere.Normalized;

		public override Float3 Render(Float2 screenUV)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(screenUV));

			Float3 energy = Float3.one;
			Float3 light = Float3.zero;

			for (int bounce = 0; bounce < profile.maxBounce; bounce++)
			{
				if (!GetIntersection(ray, out Hit hit)) break;

				CalculatedHit calculated = new CalculatedHit(hit, ray, profile.pressed);
				MaterialNew material = profile.pressed.GetMaterial(hit);

				ExtendedRandom random = Random;

				Float3 emission = material.Emit(calculated, random);
				Float3 bsdf = material.BidirectionalScatter(calculated, random, out Float3 direction);

				light += energy * emission;
				energy *= bsdf;

				if (energy <= profile.energyEpsilon) break;
			}

			throw new NotImplementedException();
		}
	}

	public readonly struct CalculatedHit
	{
		public CalculatedHit(in Hit hit, in Ray ray, PressedScene scene)
		{
			position = ray.GetPoint(hit.distance);
			direction = ray.direction;

			normal = scene.GetNormal(hit);
			uv = scene.GetTexcoord(hit);

			distance = hit.distance;
		}

		public readonly Float3 position;
		public readonly Float3 direction;

		public readonly Float3 normal;
		public readonly Float2 uv;

		public readonly float distance;
	}
}