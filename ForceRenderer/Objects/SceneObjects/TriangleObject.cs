using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class TriangleObject : SceneObject
	{
		public TriangleObject(Material material, Float3 vertex0, Float3 vertex1, Float3 vertex2) : base(material)
		{
			Vertex0 = vertex0;
			Vertex1 = vertex1;
			Vertex2 = vertex2;
		}

		public Float3 Vertex0 { get; set; }
		public Float3 Vertex1 { get; set; }
		public Float3 Vertex2 { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(int materialToken)
		{
			yield return new PressedTriangle(this, materialToken);
		}

		public override IEnumerable<PressedSphere> ExtractSpheres(int materialToken) => Enumerable.Empty<PressedSphere>();
	}

	public readonly struct PressedTriangle //Winding order for triangles is CLOCKWISE
	{
		public PressedTriangle(TriangleObject triangle, int materialToken) : this
		(
			triangle.LocalToWorld.MultiplyPoint(triangle.Vertex0),
			triangle.LocalToWorld.MultiplyPoint(triangle.Vertex1),
			triangle.LocalToWorld.MultiplyPoint(triangle.Vertex2),
			materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2, int materialToken)
		{
			this.vertex0 = vertex0;
			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;

			normal = Float3.Cross(edge1, edge2).Normalized;
			this.materialToken = materialToken;
		}

		public readonly Float3 vertex0; //NOTE: Vertex one and two are actually not needed for intersection
		public readonly Float3 edge1;   //but we can easily add them back if needed
		public readonly Float3 edge2;

		public readonly Float3 normal;
		public readonly int materialToken;

		public const float Epsilon = 1E-7f;

		public Float3 Vertex1 => vertex0 + edge1;
		public Float3 Vertex2 => vertex0 + edge2;

		public AxisAlignedBoundingBox AABB
		{
			get
			{
				Float3 vertex1 = Vertex1;
				Float3 vertex2 = Vertex2;

				Float3 min = vertex0.Min(vertex1).Min(vertex2);
				Float3 max = vertex0.Max(vertex1).Max(vertex2);

				Float3 extend = (max - min) / 2f;
				return new AxisAlignedBoundingBox(min + extend, extend);
			}
		}

		/// <summary>
		/// Returns the area of the triangle using Heron's formula.
		/// </summary>
		public float Area
		{
			get
			{
				float distance0 = (edge1 - edge2).Magnitude;
				float distance1 = edge1.Magnitude;
				float distance2 = edge2.Magnitude;

				float sum = (distance0 + distance1 + distance2) / 2f;
				float value = sum * (sum - distance0) * (sum - distance1) * (sum - distance2);

				return value <= 0f ? 0f : MathF.Sqrt(value); //To avoid square rooting negative values due to floating point precision issues
			}
		}

		public float GetIntersection(in Ray ray)
		{
			Float3 cross0 = Float3.Cross(ray.direction, edge2); //Calculating determinant and u
			float determinant = Float3.Dot(edge1, cross0);      //If determinant is close to zero, ray is parallel to triangle

			if (determinant < Epsilon) return float.PositiveInfinity; //No intersection

			Float3 offset = ray.origin - vertex0;
			float u = Float3.Dot(offset, cross0);

			if (u < 0f || u > determinant) return float.PositiveInfinity; //Outside barycentric bounds

			Float3 cross1 = Float3.Cross(offset, edge1);
			float v = Float3.Dot(ray.direction, cross1);

			if (v < 0f || u + v > determinant) return float.PositiveInfinity; //Outside barycentric bounds

			float distance = Float3.Dot(edge2, cross1);
			if (distance < 0f) return float.PositiveInfinity; //Ray pointing away from triangle = negative distance

			float inverse = 1f / determinant;

#if false //Wireframe
			u *= inverse;
			v *= inverse;

			const float WireframeThreshold = 0.1f;
			if (u > WireframeThreshold && v > WireframeThreshold && 1f - u - v > WireframeThreshold) return float.PositiveInfinity;
#endif

			return distance * inverse;
		}

		public Float3 GetNormal() => normal;
	}
}