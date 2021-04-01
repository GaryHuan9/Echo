using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Mathematics.Intersections;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Objects.GeometryObjects
{
	public class TriangleObject : GeometryObject
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

		public override IEnumerable<PressedTriangle> ExtractTriangles(Func<Material, int> materialConverter)
		{
			int materialToken = materialConverter(Material);

			if (Normal0 == Float3.zero || Normal1 == Float3.zero || Normal2 == Float3.zero)
			{
				yield return new PressedTriangle
				(
					LocalToWorld.MultiplyPoint(Vertex0), LocalToWorld.MultiplyPoint(Vertex1), LocalToWorld.MultiplyPoint(Vertex2),
					materialToken
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

		public override IEnumerable<PressedSphere> ExtractSpheres(Func<Material, int> materialConverter) => Enumerable.Empty<PressedSphere>();
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
			normal, normal, normal, materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2,
							   Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, int materialToken) : this
		(
			vertex0, vertex1, vertex2,
			Float3.Cross(vertex1 - vertex0, vertex2 - vertex0),
			texcoord0, texcoord1, texcoord2, materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2,
							   Float3 normal,
							   Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, int materialToken) : this
		(
			vertex0, vertex1, vertex2,
			normal, normal, normal,
			texcoord0, texcoord1, texcoord2, materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2,
							   Float3 normal0, Float3 normal1, Float3 normal2, int materialToken) : this
		(
			vertex0, vertex1, vertex2,
			normal0, normal1, normal2,
			Float2.zero, Float2.zero, Float2.zero, materialToken
		) { }

		public PressedTriangle(Float3 vertex0, Float3 vertex1, Float3 vertex2,
							   Float3 normal0, Float3 normal1, Float3 normal2,
							   Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, int materialToken)
		{
			this.vertex0 = vertex0;
			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;

			this.normal0 = normal0.Normalized;
			this.normal1 = normal1.Normalized;
			this.normal2 = normal2.Normalized;

			this.texcoord0 = texcoord0;
			this.texcoord1 = texcoord1;
			this.texcoord2 = texcoord2;

			this.materialToken = materialToken;
		}

		public readonly Float3 vertex0; //NOTE: Vertex one and two are actually not needed for intersection
		public readonly Float3 edge1;
		public readonly Float3 edge2;

		public readonly Float3 normal0;
		public readonly Float3 normal1;
		public readonly Float3 normal2;

		public readonly Float2 texcoord0;
		public readonly Float2 texcoord1;
		public readonly Float2 texcoord2;

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

		/// <summary>
		/// Returns the distance of intersection between this triangle and <paramref name="ray"/> without backface culling.
		/// Uses the famous Möller–Trumbore algorithm: https://cadxfem.org/inf/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf.
		/// <paramref name="uv"/> contains the barycentric position of the intersection; if intersection does not exist, infinity is returned.
		/// </summary>
		public float GetIntersection(in Ray ray, out Float2 uv)
		{
			const float Margin = 0f; //Used to easily differentiate adjacent triangles

			Float3 cross2 = Float3.Cross(ray.direction, edge2); //Calculating determinant and u
			float determinant = Float3.Dot(edge1, cross2);      //If determinant is close to zero, ray is parallel to triangle

			if (determinant > -Epsilon && determinant < Epsilon) goto noIntersection;
			float inverse = 1f / determinant;

			Float3 offset = ray.origin - vertex0;
			float u = offset.Dot(cross2) * inverse;

			if (u < Margin || u > 1f - Margin) goto noIntersection; //Outside barycentric bounds

			Float3 cross1 = Float3.Cross(offset, edge1);
			float v = ray.direction.Dot(cross1) * inverse;

			if (v < Margin || u + v > 1f - Margin) goto noIntersection; //Outside barycentric bounds

			float distance = edge2.Dot(cross1) * inverse;
			if (distance < 0f) goto noIntersection; //Ray pointing away from triangle = negative distance

			uv = new Float2(u, v);
			return distance;

			noIntersection:
			uv = default;
			return float.PositiveInfinity;
		}

		public Float3 GetVertex(Float2 uv) => vertex0 + uv.x * edge1 + uv.y * edge2;
		public Float3 GetNormal(Float2 uv) => ((1f - uv.x - uv.y) * normal0 + uv.x * normal1 + uv.y * normal2).Normalized;
		public Float2 GetTexcoord(Float2 uv) => (1f - uv.x - uv.y) * texcoord0 + uv.x * texcoord1 + uv.y * texcoord2;

		public void GetSubdivided(Span<PressedTriangle> triangles, int iteration)
		{
			int requiredLength = 1 << (iteration * 2);

			if (triangles.Length == requiredLength)
			{
				triangles[0] = this;
				GetSubdivided(triangles, normal0, normal1, normal2);
			}
			else throw ExceptionHelper.Invalid(nameof(triangles), triangles.Length, $"is not long enough! Need at least {requiredLength}!");
		}

		public override string ToString() => $"<{nameof(vertex0)}: {vertex0}, {nameof(Vertex1)}: {Vertex1}, {nameof(Vertex2)}: {Vertex2}>";

		//The uv locations right in the middle of two vertices
		static readonly Float2 uv01 = new Float2(0.5f, 0f);
		static readonly Float2 uv02 = new Float2(0f, 0.5f);
		static readonly Float2 uv12 = new Float2(0.5f, 0.5f);

		static void GetSubdivided(Span<PressedTriangle> triangles, Float3 normal0, Float3 normal1, Float3 normal2)
		{
			if (triangles.Length <= 1) return;
			PressedTriangle triangle = triangles[0];

			Float3 vertex01 = triangle.GetVertex(uv01);
			Float3 vertex02 = triangle.GetVertex(uv02);
			Float3 vertex12 = triangle.GetVertex(uv12);

			Float3 normal01 = GetInterpolatedNormal(uv01);
			Float3 normal02 = GetInterpolatedNormal(uv02);
			Float3 normal12 = GetInterpolatedNormal(uv12);

			Float2 texcoord01 = triangle.GetTexcoord(uv01);
			Float2 texcoord02 = triangle.GetTexcoord(uv02);
			Float2 texcoord12 = triangle.GetTexcoord(uv12);

			Fill(triangles, 0, triangle.materialToken, vertex01, vertex12, vertex02, normal01, normal12, normal02, texcoord01, texcoord12, texcoord02);
			Fill(triangles, 1, triangle.materialToken, triangle.vertex0, vertex01, vertex02, normal0, normal01, normal02, triangle.texcoord0, texcoord01, texcoord02);
			Fill(triangles, 2, triangle.materialToken, triangle.Vertex1, vertex12, vertex01, normal1, normal12, normal01, triangle.texcoord1, texcoord12, texcoord01);
			Fill(triangles, 3, triangle.materialToken, triangle.Vertex2, vertex02, vertex12, normal2, normal02, normal12, triangle.texcoord2, texcoord02, texcoord12);

			//NOTE: this normal is not normalized, because normalized normals will mess up during subdivision
			Float3 GetInterpolatedNormal(Float2 uv) => (1f - uv.x - uv.y) * normal0 + uv.x * normal1 + uv.y * normal2;

			static void Fill(Span<PressedTriangle> span, int index, int materialToken,
							 Float3 vertex0, Float3 vertex1, Float3 vertex2,
							 Float3 normal0, Float3 normal1, Float3 normal2,
							 Float2 texcoord0, Float2 texcoord1, Float2 texcoord2)
			{
				int gap = span.Length / 4;
				var slice = span.Slice(gap * index, gap);

				slice[0] = new PressedTriangle
				(
					vertex0, vertex1, vertex2,
					normal0, normal1, normal2,
					texcoord0, texcoord1, texcoord2, materialToken
				);

				GetSubdivided(slice, normal0, normal1, normal2);
			}
		}
	}
}