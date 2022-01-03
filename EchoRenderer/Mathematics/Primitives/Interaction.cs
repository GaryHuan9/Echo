using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Mathematics.Primitives
{
	public struct Interaction
	{
		public Interaction(in TraceQuery query, in Float3 geometryNormal, in Float3 normal, in Float2 texcoord)
		{
			query.AssertHit();

			token = query.token;
			position = query.Position;
			outgoingWorld = -query.ray.direction;

			this.geometryNormal = geometryNormal;
			this.normal = normal;
			this.texcoord = texcoord;
			bsdf = null;
		}

		/// <summary>
		/// The <see cref="GeometryToken"/> that represents the geometry that we are interacting with.
		/// </summary>
		public readonly GeometryToken token;

		/// <summary>
		/// The <see cref="position"/> at which the <see cref="Interaction"/> occured.
		/// </summary>
		public readonly Float3 position;

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

		/// <summary>
		/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Interaction"/> towards <paramref name="direction"/>.
		/// </summary>
		public readonly TraceQuery SpawnTrace(in Float3 direction) => new(new Ray(position, direction), float.PositiveInfinity, token);

		/// <summary>
		/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Interaction"/> with a direction directly opposite to <see cref="outgoingWorld"/>.
		/// </summary>
		public readonly TraceQuery SpawnTrace() => SpawnTrace(-outgoingWorld);

		/// <summary>
		/// Spawns a new <see cref="OccludeQuery"/> with <paramref name="direction"/> and <paramref name="travel"/>.
		/// </summary>
		public readonly OccludeQuery SpawnOcclude(in Float3 direction, float travel = float.PositiveInfinity) => new(new Ray(position, direction), travel, token);

		/// <summary>
		/// Spawns a new <see cref="OccludeQuery"/> directly opposite to <see cref="outgoingWorld"/> with <paramref name="travel"/>.
		/// </summary>
		public readonly OccludeQuery SpawnOcclude(float travel = float.PositiveInfinity) => SpawnOcclude(-outgoingWorld, travel);
	}
}