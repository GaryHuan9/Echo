using CodeHelpers.Mathematics;

namespace ForceRenderer.Objects
{
	/// <summary>
	/// A directional light. Infinitely far away pointed towards the local positive z axis.
	/// </summary>
	public class Light : Object
	{
		public Float3 Intensity { get; set; } = Float3.one;
	}

	public readonly struct PressedLight
	{
		public PressedLight(Light light)
		{
			direction = light.LocalToWorld.MultiplyDirection(Float3.forward);
			intensity = light.Intensity.Max(Float3.zero);
		}

		public readonly Float3 direction;
		public readonly Float3 intensity;
	}
}