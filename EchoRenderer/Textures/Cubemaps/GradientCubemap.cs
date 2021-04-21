using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures.Cubemaps
{
	public class GradientCubemap : Cubemap
	{
		/// <summary>
		/// Creates a cubemap that samples color based on the y direction.
		/// </summary>
		/// <param name="gradient">The gradient is only sampled from 0 to 1.</param>
		public GradientCubemap(Gradient gradient) => this.gradient = gradient;

		/// <summary>
		/// Whether the color sampling uses the direction angles or the direction directly.
		/// </summary>
		public bool Angular { get; set; } = true;

		readonly Gradient gradient;

		public override Float3 Sample(in Float3 direction)
		{
			float percent = GetPercent(direction);
			return gradient[percent].XYZ;
		}

		float GetPercent(in Float3 direction)
		{
			if (!Angular) return direction.y / 2f + 0.5f;
			return Float3.Angle(Float3.down, direction) / 180f;
		}
	}
}