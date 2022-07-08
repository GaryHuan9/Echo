using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Geometries;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Query for the traversal of a <see cref="Ray"/> to find out whether anything inside <see cref="PreparedScene"/> occludes its <see cref="travel"/> (distance).
/// NOTE: the only output of this query is a <see cref="bool"/> indicating any occlusion; more information can be accessed using a <see cref="TraceQuery"/>.
/// </summary>
public struct OccludeQuery
{
	public OccludeQuery(in Ray ray, float travel = float.PositiveInfinity, in TokenHierarchy ignore = default)
	{
		this.ray = ray;
		this.ignore = ignore;
		this.travel = travel;
		current = new TokenHierarchy();
	}

	/// <summary>
	/// The main ray of this <see cref="OccludeQuery"/>. Note that this will be changed and updated
	/// as we traverse through the scene when we go from a parent coordinate system to a child system.
	/// </summary>
	public Ray ray;

	/// <summary>
	/// The <see cref="TokenHierarchy"/> that represents a geometry that this <see cref="OccludeQuery"/> should ignore.
	/// This should mainly be assigned to the <see cref="TokenHierarchy"/> of the previous <see cref="TraceQuery"/> to
	/// avoid self intersections. Note that if the geometry is a <see cref="PreparedSphere"/>, then it will only be
	/// ignored if its farthest distance is shorter than <see cref="PreparedSphere.DistanceThreshold"/>.
	/// </summary>
	public readonly TokenHierarchy ignore;

	/// <summary>
	/// Used during intersection test; undefined after the test is concluded (do not use upon completion).
	/// Records the intermediate instancing layers as the query travels through the scene and geometries.
	/// </summary>
	public TokenHierarchy current;

	/// <summary>
	/// Only occluding geometries that is within <see cref="travel"/> is considered. Note that
	/// the numerical value of this field will change as we move between coordinate systems.
	/// </summary>
	public float travel;
}