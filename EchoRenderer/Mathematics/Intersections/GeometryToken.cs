using System;
using EchoRenderer.Mathematics.Accelerators;

namespace EchoRenderer.Mathematics.Intersections
{
	public readonly struct GeometryToken : IEquatable<GeometryToken>
	{
		public GeometryToken(PressedInstance instance, uint geometry)
		{
			this.instance = instance.id;
			this.geometry = geometry;
		}

		/// <summary>
		/// The id of the <see cref="PressedInstance"/> that contains this particular geometry.
		/// </summary>
		public readonly uint instance;

		/// <summary>
		/// The unique token for one geometry inside a <see cref="PressedPack"/>.
		/// </summary>
		public readonly uint geometry;

		public bool Equals(GeometryToken other) => instance == other.instance && geometry == other.geometry;

		public override bool Equals(object obj) => obj is GeometryToken other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int)instance * 397) ^ (int)geometry;
			}
		}

		public static bool operator ==(GeometryToken token0, GeometryToken token1) => token0.Equals(token1);
		public static bool operator !=(GeometryToken token0, GeometryToken token1) => !token0.Equals(token1);
	}
}