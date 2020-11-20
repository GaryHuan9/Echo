using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class DirectionalLight : Object
	{
		public Float3 Intensity { get; set; } = Float3.one;
	}

	public readonly struct PressedDirectionalLight
	{
		public PressedDirectionalLight(DirectionalLight directionalLight)
		{
			direction = directionalLight.LocalToWorld.MultiplyDirection(Float3.forward);
			intensity = directionalLight.Intensity.Max(Float3.zero);
		}

		public readonly Float3 direction;
		public readonly Float3 intensity;
	}
}