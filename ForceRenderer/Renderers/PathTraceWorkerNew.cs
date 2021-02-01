using CodeHelpers.Mathematics;
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

			for (int bounce = 0; bounce < profile.maxBounce; bounce++)
			{
				if (!GetIntersection(ray, out Hit hit)) break;

				Float3 position = ray.GetPoint(hit.distance);
				Float3 normal = profile.pressed.GetNormal(hit);

				Float3 direction;
				Float3 bsdf; //Bidirectional scattering distribution function
			}
		}
	}
}