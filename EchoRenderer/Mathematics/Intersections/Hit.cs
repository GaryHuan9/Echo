using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Mutable data struct used during intersection tests to distribute hit information.
	/// </summary>
	public struct Hit
	{
		public PressedPackInstance instance;
		public Float3 normal;

		public float distance;
		public uint token;
		public Float2 uv;
	}

	/// <summary>
	/// A formally-sealed hit information.
	/// </summary>
	public readonly struct CalculatedHit
	{
		public CalculatedHit(in Float3 position, in Float3 direction, float distance, Material material, in Float3 normal, Float2 texcoord)
		{
			Assert.IsTrue(Scalars.AlmostEquals(direction.SquaredMagnitude, 1f));
			Assert.IsTrue(Scalars.AlmostEquals(normal.SquaredMagnitude, 1f));

			this.position = position;
			this.direction = direction;
			this.distance = distance;

			this.material = material;
			this.normal = normal;
			this.texcoord = texcoord;

			material.ApplyTangentNormal(this, ref this.normal);
		}

		public readonly Float3 position;
		public readonly Float3 direction;
		public readonly float distance;

		public readonly Material material;
		public readonly Float3 normal;
		public readonly Float2 texcoord;
	}
}