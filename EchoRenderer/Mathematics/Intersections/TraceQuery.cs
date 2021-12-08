using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Query for the trace of a <see cref="Ray"/>.
	/// </summary>
	public struct TraceQuery
	{
		public TraceQuery(in Ray ray, GeometryToken previous = default)
		{
			this.ray = ray;
			this.previous = previous;

			current = default;
			token = default;
			distance = float.PositiveInfinity;
			uv = default;
		}

		/// <summary>
		/// The main ray of this <see cref="TraceQuery"/>. Note that this will be changed and updated
		/// as we trace through the scene when we go from a parent coordinate system to a child system.
		/// </summary>
		public Ray ray;

		/// <summary>
		/// The <see cref="GeometryToken"/> of the previous <see cref="TraceQuery"/>. Or default initialize this if
		/// there is no previous <see cref="TraceQuery"/>. This is used to determine and avoid duplicated intersections.
		/// </summary>
		public readonly GeometryToken previous;

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
		/// NOTE: if there is no intersection with the scene, this field will be assigned <see cref="float.PositiveInfinity"/>.
		/// </summary>
		public float distance;

		/// <summary>
		/// After tracing completes, this field will be assigned the local position/coordinate on the specific
		/// parametrization of the intersected surface. NOTE: if no intersection occurs, this field is undefined.
		/// </summary>
		public Float2 uv;

		/// <summary>
		/// Returns whether this <see cref="TraceQuery"/> has intersected with anything.
		/// </summary>
		public readonly bool Hit => distance < float.PositiveInfinity;

		/// <summary>
		/// Spawns and returns a new <see cref="TraceQuery"/> from the result of this <see cref="TraceQuery"/> towards <paramref name="direction"/>.
		/// </summary>
		public readonly TraceQuery Next(Float3 direction)
		{
			Assert.IsTrue(Hit);
			return new TraceQuery(new Ray(ray.GetPoint(distance), direction), token);
		}

		/// <summary>
		/// Spans and returns a new <see cref="TraceQuery"/> with the same direction from the result of this <see cref="TraceQuery"/>.
		/// </summary>
		public readonly TraceQuery Next() => Next(ray.direction);

		public static implicit operator TraceQuery(in Ray ray) => new(ray);
	}
}