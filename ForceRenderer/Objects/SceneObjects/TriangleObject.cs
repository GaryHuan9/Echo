using System;
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
	}

	public readonly struct PressedTriangle //Winding order for triangles is CLOCKWISE
	{
		public PressedTriangle(TriangleObject triangle, int materialToken)
		{
			vertex0 = triangle.LocalToWorld.MultiplyPoint(triangle.Vertex0);
			vertex1 = triangle.LocalToWorld.MultiplyPoint(triangle.Vertex1);
			vertex2 = triangle.LocalToWorld.MultiplyPoint(triangle.Vertex2);

			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;

			normal = Float3.Cross(edge1, edge2).Normalized;
			this.materialToken = materialToken;
		}

		public readonly Float3 vertex0;
		public readonly Float3 vertex1;
		public readonly Float3 vertex2;

		public readonly Float3 edge1;
		public readonly Float3 edge2;

		public readonly Float3 normal;
		public readonly int materialToken;

		public const float Epsilon = 1E-7f;

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

			// u *= inverse; Currently not used
			// v *= inverse;

			return distance * inverse;
		}

		public Float3 GetNormal() => normal;
	}
}