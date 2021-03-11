using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Rendering
{
	public class AlbedoPixelWorker : PixelWorker
	{
		public AlbedoPixelWorker(RenderEngine.Profile profile) : base(profile) { }

		public override Float3 Render(Float2 screenUV)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(screenUV));

			while (GetIntersection(ray, out Hit hit))
			{
				CalculatedHit calculated = new CalculatedHit(hit, ray, profile.pressed);
				Material material = profile.pressed.GetMaterial(hit);

				Float4 sample = material.AlbedoMap[calculated.texcoord];
				if (!Scalars.AlmostEquals(sample.w, 0f)) return sample.XYZ * material.Albedo;

				ray = new Ray(calculated.position, calculated.direction, true);
			}

			return profile.scene.Cubemap?.Sample(ray.direction) ?? Float3.zero;
		}
	}
}