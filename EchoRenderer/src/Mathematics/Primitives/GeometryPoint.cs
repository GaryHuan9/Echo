using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Primitives
{
	/// <summary>
	/// Contains geometric information about a point on a geometry.
	/// </summary>
	public readonly struct GeometryPoint
	{
		public GeometryPoint(in Float3 position, in Float3 normal)
		{
			Assert.AreEqual(normal.SquaredMagnitude, 1f);

			this.position = position;
			this.normal = normal;
		}

		/// <summary>
		/// The world space position of this point.
		/// </summary>
		public readonly Float3 position;

		/// <summary>
		/// The world space surface normal of the geometry at this point.
		/// </summary>
		public readonly Float3 normal;
	}
}