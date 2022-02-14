using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Rendering.Scattering;

namespace EchoRenderer.Core.Aggregation.Primitives;

public struct Interaction
{
	public Interaction(in TraceQuery query, in Float3 normal) : this
	(
		query, new GeometryPoint(query.Position, normal)
	) { }

	public Interaction(in TraceQuery query, in Float3 normal, Material material, Float2 texcoord) : this
	(
		query,
		new GeometryPoint(query.Position, normal),
		new GeometryShade(material, texcoord, normal)
	) { }

	public Interaction(in TraceQuery query, in GeometryPoint point, in GeometryShade shade = default)
	{
		query.AssertHit();
		token = query.token;
		outgoing = -query.ray.direction;

		this.point = point;
		this.shade = shade;

		bsdf = null;
	}

	/// <summary>
	/// The <see cref="GeometryToken"/> that represents the geometry that we are interacting with.
	/// </summary>
	public readonly GeometryToken token;

	/// <summary>
	/// World space outgoing direction of this <see cref="Interaction"/>.
	/// </summary>
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
	/// The <see cref="BSDF"/> of this <see cref="Interaction"/>.
	/// </summary>
	public BSDF bsdf;

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Interaction"/> towards <paramref name="direction"/>.
	/// </summary>
	public readonly TraceQuery SpawnTrace(in Float3 direction) => new(new Ray(point.position, direction), float.PositiveInfinity, token);

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> from this <see cref="Interaction"/> with a direction directly opposite to <see cref="outgoing"/>.
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
	/// Converts to the position of <paramref name="interaction"/>.
	/// </summary>
	public static implicit operator Float3(in Interaction interaction) => interaction.point.position;
}