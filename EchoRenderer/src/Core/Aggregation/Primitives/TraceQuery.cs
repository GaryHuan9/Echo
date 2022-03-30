using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation.Primitives;

/// <summary>
/// Query for the traverse of a <see cref="Ray"/> to find out whether an intersection with a <see cref="PreparedScene"/>
/// exists, and if it does, the exact distance of that intersections and other specific information about it.
/// </summary>
public struct TraceQuery
{
	public TraceQuery(in Ray ray, float distance = float.PositiveInfinity, in GeometryToken ignore = default)
	{
		this.ray = ray;
		this.ignore = ignore;
		this.distance = distance;

		current = default;
		Unsafe.SkipInit(out token);
		Unsafe.SkipInit(out uv);

#if DEBUG
		originalDistance = distance;
#endif
	}

	/// <summary>
	/// The main ray of this <see cref="TraceQuery"/>. Note that this will be changed and updated
	/// as we traverse through the scene when we go from a parent coordinate system to a child system.
	/// </summary>
	public Ray ray;

	/// <summary>
	/// The <see cref="GeometryToken"/> that represents a geometry that this <see cref="TraceQuery"/> should ignore.
	/// This should mainly be assigned to the <see cref="GeometryToken"/> of the previous <see cref="TraceQuery"/> to
	/// avoid self intersections. Note that if the geometry is a <see cref="PreparedSphere"/>, then it will only be
	/// ignored if its farthest distance is shorter than <see cref="PreparedSphere.DistanceThreshold"/>.
	/// </summary>
	public readonly GeometryToken ignore;

	/// <summary>
	/// Used during intersection test; undefined after the test is concluded (do not use upon completion).
	/// Records the intermediate instancing layers as the query travels through the scene and geometries.
	/// </summary>
	public GeometryToken current;

	/// <summary>
	/// After tracing completes, this field will be assigned the <see cref="GeometryToken"/>
	/// of the intersected surface. NOTE: if no intersection occurs, this field is undefined.
	/// </summary>
	public GeometryToken token;

	/// <summary>
	/// After tracing completes, this field will be assigned the distance of the intersection to the <see cref="ray"/> origin.
	/// NOTE: if there is no intersection with the scene, this field will be kept as its originally constructed value.
	/// </summary>
	public float distance;

	/// <summary>
	/// After tracing completes, this field will be assigned the local position/coordinate on the specific
	/// parametrization of the intersected surface. NOTE: if no intersection occurs, this field is undefined.
	/// </summary>
	public Float2 uv;

	/// <summary>
	/// After tracing completes, if an intersection occurred, returns its <see cref="Position"/>, otherwise this property is undefined.
	/// NOTE: if <see cref="distance"/> is really small, this property will be shifted slightly along <see cref="Ray.direction"/>, to
	/// avoid us spawning new <see cref="TraceQuery"/> or <see cref="OccludeQuery"/> on the exact same <see cref="Ray.origin"/>.
	/// </summary>
	public readonly Float3 Position
	{
		get
		{
			AssertHit();
			return ray.GetPoint(Math.Max(distance, Scalars.Epsilon));
		}
	}

#if DEBUG
	/// <summary>
	/// The immutable original distance to determine whether this <see cref="TraceQuery"/> actually <see cref="Hit"/> something.
	/// </summary>
	readonly float originalDistance;

	/// <summary>
	/// Returns whether this <see cref="TraceQuery"/> has intersected with anything.
	/// </summary>
	public readonly bool Hit => distance < originalDistance;
#endif

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> from the result of this <see cref="TraceQuery"/> towards <paramref name="direction"/>.
	/// </summary>
	public readonly TraceQuery SpawnTrace(in Float3 direction) => new(new Ray(Position, direction), float.PositiveInfinity, token);

	/// <summary>
	/// Spawns a new <see cref="TraceQuery"/> with the same direction from the result of this <see cref="TraceQuery"/>.
	/// </summary>
	public readonly TraceQuery SpawnTrace() => SpawnTrace(ray.direction);

	/// <summary>
	/// Spawns a new <see cref="OccludeQuery"/> from the result of this <see cref="TraceQuery"/> towards <paramref name="direction"/>.
	/// </summary>
	public readonly OccludeQuery SpawnOcclude(in Float3 direction, float travel = float.PositiveInfinity) => new(new Ray(Position, direction), travel, token);

	/// <summary>
	/// Spawns a new <see cref="OccludeQuery"/> with the same direction from the result of this <see cref="TraceQuery"/>.
	/// </summary>
	public readonly OccludeQuery SpawnOcclude(float travel = float.PositiveInfinity) => SpawnOcclude(ray.direction, travel);

	/// <summary>
	/// Ensures that this <see cref="TraceQuery"/> has <see cref="Hit"/> something.
	/// </summary>
	[Conditional(Assert.DebugSymbol)]
	public readonly void AssertHit()
	{
#if DEBUG
		Assert.IsTrue(Hit);
#endif
	}
}