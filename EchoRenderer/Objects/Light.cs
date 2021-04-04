using CodeHelpers.Mathematics;

namespace EchoRenderer.Objects
{
	/// <summary>
	/// A directional light. Infinitely far away pointed towards the local positive z axis.
	/// </summary>
	public class Light : Object
	{
		public Float3 Intensity { get; set; } = Float3.one;

		/// <summary>
		/// How large is this directional light? One means the light covers half of the sky.
		/// Zero means the light is a single point, which is impossible to hit so do not use that.
		/// </summary>
		public float Coverage { get; set; } = 0.5f;
	}

	public readonly struct PressedLight
	{
		public PressedLight(Light light)
		{
			direction = light.LocalToWorld.MultiplyDirection(Float3.forward).Normalized;
			intensity = light.Intensity.Max(Float3.zero);

			threshold = 1f - light.Coverage.Clamp(0f, 1f);
		}

		public readonly Float3 direction;
		public readonly Float3 intensity;

		public readonly float threshold;
	}
}