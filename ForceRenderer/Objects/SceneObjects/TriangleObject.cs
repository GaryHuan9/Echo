using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
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

		public Float3 Normal0 { get; set; }
		public Float3 Normal1 { get; set; }
		public Float3 Normal2 { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(int materialToken)
		{
			if (Normal0 == Float3.zero || Normal1 == Float3.zero || Normal2 == Float3.zero)
			{
				yield return new PressedTriangle
				(
					LocalToWorld.MultiplyPoint(Vertex0),
					LocalToWorld.MultiplyPoint(Vertex1),
					LocalToWorld.MultiplyPoint(Vertex2), materialToken
				);
			}
			else
			{
				yield return new PressedTriangle
				(
					LocalToWorld.MultiplyPoint(Vertex0), LocalToWorld.MultiplyPoint(Vertex1), LocalToWorld.MultiplyPoint(Vertex2),
					LocalToWorld.MultiplyDirection(Normal0), LocalToWorld.MultiplyDirection(Normal1), LocalToWorld.MultiplyDirection(Normal2),
					materialToken
				);
			}
		}

		public override IEnumerable<PressedSphere> ExtractSpheres(int materialToken) => Enumerable.Empty<PressedSphere>();
	}

	public readonly struct PressedTriangle //Winding order for triangles is CLOCKWISE
	{
		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2, int materialToken) : this
		(
			vertex0, vertex1, vertex2,
			Float3.Cross(vertex1 - vertex0, vertex2 - vertex0), materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2, Float3 normal, int materialToken) : this
		(
			vertex0, vertex1, vertex2,
			normal, normal, normal,
			materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2,
							   Float3 normal0, Float3 normal1, Float3 normal2, int materialToken)
		{
			this.vertex0 = vertex0;
			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;

			this.normal0 = normal0.Normalized;
			this.normal1 = normal1.Normalized;
			this.normal2 = normal2.Normalized;

			this.materialToken = materialToken;
		}

		public readonly Float3 vertex0; //NOTE: Vertex one and two are actually not needed for intersection
		public readonly Float3 edge1;   //but we can easily add them back if needed
		public readonly Float3 edge2;

		public readonly Float3 normal0;
		public readonly Float3 normal1;
		public readonly Float3 normal2;

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

		public float GetIntersection(in Ray ray, out Float2 uv)
		{
			Float3 cross0 = Float3.Cross(ray.direction, edge2); //Calculating determinant and u
			float determinant = Float3.Dot(edge1, cross0);      //If determinant is close to zero, ray is parallel to triangle

			if (determinant < Epsilon) goto noIntersection;

			Float3 offset = ray.origin - vertex0;
			float u = Float3.Dot(offset, cross0);

			if (u < 0f || u > determinant) goto noIntersection; //Outside barycentric bounds

			Float3 cross1 = Float3.Cross(offset, edge1);
			float v = Float3.Dot(ray.direction, cross1);

			if (v < 0f || u + v > determinant) goto noIntersection; //Outside barycentric bounds

			float distance = Float3.Dot(edge2, cross1);
			if (distance < 0f) goto noIntersection; //Ray pointing away from triangle = negative distance

			float inverse = 1f / determinant;
			uv = new Float2(u * inverse, v * inverse);

#if false //Wireframe
			const float WireframeThreshold = 0.1f;
			if (uv.x > WireframeThreshold && uv.y > WireframeThreshold && 1f - uv.x - uv.y > WireframeThreshold) return float.PositiveInfinity;
#endif

			return distance * inverse;

			noIntersection:
			uv = default;
			return float.PositiveInfinity;
		}

		public Float3 GetNormal(Float2 uv) => ((1f - uv.x - uv.y) * normal0 + uv.x * normal1 + uv.y * normal2).Normalized;

		//The uv locations right in the middle of two vertices
		static readonly Float2 uv01 = new Float2(0.5f, 0f);
		static readonly Float2 uv02 = new Float2(0f, 0.5f);
		static readonly Float2 uv12 = new Float2(0.5f, 0.5f);

		public void GetSubdivided(Span<PressedTriangle> triangles, int iteration)
		{
			int requiredLength = 1 << (iteration * 2);
			if (triangles.Length < requiredLength) throw ExceptionHelper.Invalid(nameof(triangles), triangles.Length, $"is not long enough! Need at least {requiredLength}!");

			Float3 midPoint01 = vertex0 + edge1 / 2f;
			Float3 midPoint02 = vertex0 + edge2 / 2f;
			Float3 midPoint12 = vertex0 + edge1 / 2f + edge2 / 2f;

			Float3 normal01 = GetNormal(uv01);
			Float3 normal02 = GetNormal(uv02);
			Float3 normal12 = GetNormal(uv12);

			//TODO: texcoords

			triangles[0] = new PressedTriangle(midPoint01, midPoint12, midPoint02, normal01, normal12, normal02, materialToken);
			triangles[1] = new PressedTriangle(vertex0, midPoint01, midPoint02, normal0, normal01, normal02, materialToken);
			triangles[2] = new PressedTriangle(Vertex1, midPoint12, midPoint01, normal1, normal12, normal01, materialToken);
			triangles[3] = new PressedTriangle(Vertex2, midPoint02, midPoint12, normal2, normal02, normal12, materialToken);
		}
	}
}