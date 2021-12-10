using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Rendering;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Query for the traverse of a <see cref="Ray"/> to find out whether anything inside <see cref="PressedScene"/> occludes its <see cref="travel"/> (distance).
	/// NOTE: the only output of this query is a <see cref="bool"/> indicating any occlusion; more information can be accessed using a <see cref="TraceQuery"/>.
	/// </summary>
	public struct OccludeQuery
	{
		public OccludeQuery(in Ray ray, float travel = float.PositiveInfinity, in GeometryToken ignore = default)
		{
			this.ray = ray;
			this.travel = travel;
			this.ignore = ignore;
		}

		/// <summary>
		/// The main ray of this <see cref="OccludeQuery"/>. Note that this will be changed and updated
		/// as we traverse through the scene when we go from a parent coordinate system to a child system.
		/// </summary>
		public Ray ray;

		/// <summary>
		/// Only occluding geometries that is within <see cref="travel"/> is considered. Note that
		/// the numerical value of this field will change as we move between coordinate systems.
		/// </summary>
		public float travel;

		/// <summary>
		/// The <see cref="GeometryToken"/> that represents a geometry that this <see cref="OccludeQuery"/> should ignore
		/// if we come in very close (<see cref="PressedPack.DistanceMin"/>) contact with it. This should mainly be assigned
		/// to the <see cref="GeometryToken"/> of the previous <see cref="TraceQuery"/> to avoid origin intersections.
		/// </summary>
		public readonly GeometryToken ignore;

		public static implicit operator OccludeQuery(in Ray ray) => new(ray);
	}
}