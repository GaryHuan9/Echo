using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Mathematics.Intersections
{
	public struct Interaction
	{
		public Interaction(in TraceQuery query, in Float3 geometryNormal, in Float3 normal, in Float2 texcoord)
		{
			query.AssertHit();

			uv = query.uv;
			outgoingWorld = -query.ray.direction;

			this.geometryNormal = geometryNormal;
			this.normal = normal;
			this.texcoord = texcoord;
			bsdf = null;
		}

		/// <summary>
		/// The local coordinate of this <see cref="Interaction"/> on the
		/// intersected surface based on its specific parametrization.
		/// </summary>
		public readonly Float2 uv;

		/// <summary>
		/// The outgoing direction of this <see cref="Interaction"/> in world space.
		/// </summary>
		public readonly Float3 outgoingWorld;

		/// <summary>
		/// The pure geometry normal of this <see cref="Interaction"/>.
		/// </summary>
		public readonly Float3 geometryNormal;

		/// <summary>
		/// The shading normal of this <see cref="Interaction"/>.
		/// </summary>
		public readonly Float3 normal;

		/// <summary>
		/// The texture coordinate of this <see cref="Interaction"/>.
		/// </summary>
		public readonly Float2 texcoord;

		/// <summary>
		/// The <see cref="BSDF"/> of this <see cref="Interaction"/>.
		/// </summary>
		public BSDF bsdf;
	}
}