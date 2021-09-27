using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Accelerations;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Mutable data struct used to communicate intersection information.
	/// </summary>
	public struct HitQuery
	{
		public Ray ray;
		public PressedPackInstance instance;

		public GeometryToken previous;
		public GeometryToken token;

		public float distance;
		public Float3 normal;
		public Float2 uv;

		public Shading shading;

		public readonly bool Hit => token != default;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Next(Float3 direction)
		{
			ray = new Ray(ray.GetPoint(distance), direction);

			previous = token;
			token = default;

			distance = float.PositiveInfinity;
		}

		/// <summary>
		/// Information calculated right after intersection calculation
		/// </summary>
		public struct Shading
		{
			public Float3 normal;
			public Float2 texcoord;
			public Material material;
		}

		public static implicit operator HitQuery(in Ray ray) => new() {ray = ray, distance = float.PositiveInfinity};
	}

	public readonly struct GeometryToken : IEquatable<GeometryToken>
	{
		public GeometryToken(PressedPackInstance instance, uint geometry)
		{
			this.instance = instance.id;
			this.geometry = geometry;
		}

		/// <summary>
		/// The id of the <see cref="PressedPackInstance"/> that contains this particular geometry.
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