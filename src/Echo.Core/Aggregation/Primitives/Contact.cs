using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// A mutable struct that describes an interaction with the surface of a <see cref="PreparedScene"/>,
/// constructed through a concluded <see cref="TraceQuery"/> and other information about that surface.
/// </summary>
public struct Contact
{
	public Contact(in TraceQuery query, in Float3 normal) : this
	(
		query, new GeometryPoint(query.Position, normal)
	) { }

	public Contact(in TraceQuery query, in Float3 normal, Material material, in Float3 shadingNormal, Float2 texcoord) : this
	(
		query, new GeometryPoint(query.Position, normal),
		new GeometryShade(material, shadingNormal, texcoord)
	) { }

	public Contact(in TraceQuery query, in GeometryPoint point, in GeometryShade shade = default)
	{
		query.EnsureHit();
		token = query.token;
		outgoing = -query.ray.direction;

		this.point = point;
		this.shade = shade;

		bsdf = null;
	}

	/// <summary>
	/// The <see cref="TokenHierarchy"/> that represents the geometry that we are interacting with.
	/// </summary>
	public readonly TokenHierarchy token;

	/// <summary>
	/// World-space outgoing direction of this <see cref="Contact"/>.
	/// </summary>
	/// <remarks>This is basically pointing towards the direction
	/// where this <see cref="Contact"/> came from.</remarks>
	public readonly Float3 outgoing;

	/// <summary>
	/// Geometric information about our point of interest.
	/// </summary>
	public readonly GeometryPoint point;

	/// <summary>
	/// Shading information about our point of interest.
	/// </summary>
	public readonly GeometryShade shade;

	/// <summary>
	/// The <see cref="BSDF"/> of this <see cref="Contact"/>.
	/// </summary>
	public BSDF bsdf;

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Contact"/> towards <paramref name="direction"/>.
	/// </summary>
	public readonly TraceQuery SpawnTrace(in Float3 direction) => new(new Ray(point.position, direction), float.PositiveInfinity, token);

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Contact"/> with a direction directly opposite to <see cref="outgoing"/>.
	/// </summary>
	public readonly TraceQuery SpawnTrace() => SpawnTrace(-outgoing);

	/// <summary>
	/// Spawns a new <see cref="OccludeQuery"/> with <paramref name="direction"/> and <paramref name="travel"/>.
	/// </summary>
	public readonly OccludeQuery SpawnOcclude(in Float3 direction, float travel = float.PositiveInfinity) => new(new Ray(point.position, direction), travel, token);

	/// <summary>
	/// Spawns a new <see cref="OccludeQuery"/> directly opposite to <see cref="outgoing"/> with <paramref name="travel"/>.
	/// </summary>
	public readonly OccludeQuery SpawnOcclude(float travel = float.PositiveInfinity) => SpawnOcclude(-outgoing, travel);

	/// <summary>
	/// Returns the absolute value of the dot product between <paramref name="direction"/> and <see cref="GeometryShade.Normal"/>.
	/// </summary>
	public readonly float NormalDot(in Float3 direction) => FastMath.Abs(direction.Dot(shade.Normal));

	/// <summary>
	/// Converts to the position of <paramref name="contact"/>.
	/// </summary>
	public static implicit operator Float3(in Contact contact) => contact.point.position;

	/// <summary>
	/// Preliminary information that can be used to construct a full <see cref="Contact"/>.
	/// </summary>
	public readonly struct Info
	{
		public Info(MaterialIndex material, in Float3 normal, Float3 shadingNormal, Float2 texcoord)
		{
			this.material = material;
			this.normal = normal;
			this.shadingNormal = shadingNormal;
			this.texcoord = texcoord;
		}

		public readonly MaterialIndex material;
		public readonly Float3 normal;
		public readonly Float3 shadingNormal;
		public readonly Float2 texcoord;
	}
}