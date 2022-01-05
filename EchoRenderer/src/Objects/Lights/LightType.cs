using System;

namespace EchoRenderer.Objects.Lights
{
	[Flags]
	public enum LightType
	{
		/// <summary>
		/// Sources that emit from a single point (delta light).
		/// </summary>
		position = 1 << 0,

		/// <summary>
		/// Sources that emit in a single direction (delta light).
		/// </summary>
		direction = 1 << 1,

		/// <summary>
		/// Sources that emit from the area of some geometry.
		/// </summary>
		area = 1 << 2,

		/// <summary>
		/// Sources that emit from infinitely far away.
		/// </summary>
		infinite = 1 << 3,

		/// <summary>
		/// Source with delta distributions (singularities), either <see cref="position"/> or <see cref="direction"/>.
		/// </summary>
		delta = position | direction
	}

	public static class LightTypeExtensions
	{
		/// <summary>
		/// Returns whether all the flags turned on in <paramref name="type"/> is turned on in <paramref name="other"/>.
		/// </summary>
		public static bool Fits(this LightType type, LightType other) => (type & other) == type;

		/// <summary>
		/// Returns whether any flags turned on in <paramref name="type"/> is turned on in <paramref name="other"/>.
		/// </summary>
		public static bool Any(this LightType type, LightType other) => (type & other) != 0;

		/// <summary>
		/// Returns whether this <see cref="LightType"/> <paramref name="type"/> is <see cref="LightType.delta"/>.
		/// </summary>
		public static bool IsDelta(this LightType type) => type.Any(LightType.delta);
	}
}