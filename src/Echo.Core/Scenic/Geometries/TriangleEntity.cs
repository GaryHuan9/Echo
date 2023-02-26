using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A geometric triangle.
/// </summary>
[EchoSourceUsable]
public class TriangleEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	[EchoSourceUsable] public Float3 Vertex0 { get; set; } = Float3.Zero;
	[EchoSourceUsable] public Float3 Vertex1 { get; set; } = Float3.Up;
	[EchoSourceUsable] public Float3 Vertex2 { get; set; } = Float3.One;

	[EchoSourceUsable] public Float3 ShadingNormal0 { get; set; }
	[EchoSourceUsable] public Float3 ShadingNormal1 { get; set; }
	[EchoSourceUsable] public Float3 ShadingNormal2 { get; set; }

	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		MaterialIndex material = extractor.Register(Material);
		Float4x4 transform = InverseTransform;

		Float3 normal = Float3.Cross(Vertex1 - Vertex0, Vertex2 - Vertex0);
		Float3 normal0 = ShadingNormal0 == Float3.Zero ? normal : ShadingNormal0;
		Float3 normal1 = ShadingNormal1 == Float3.Zero ? normal : ShadingNormal1;
		Float3 normal2 = ShadingNormal2 == Float3.Zero ? normal : ShadingNormal2;

		yield return new PreparedTriangle
		(
			transform.MultiplyPoint(Vertex0),
			transform.MultiplyPoint(Vertex1),
			transform.MultiplyPoint(Vertex2),
			transform.MultiplyDirection(normal0).Normalized,
			transform.MultiplyDirection(normal1).Normalized,
			transform.MultiplyDirection(normal2).Normalized,
			material
		);
	}
}

/// <summary>
/// The prepared version of a <see cref="TriangleEntity"/>.
/// </summary>
/// <remarks>Winding order is clockwise.</remarks>
public readonly struct PreparedTriangle : IPreparedGeometry
{
	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2, MaterialIndex materialIndex) : this
	(
		vertex0, vertex1, vertex2,
		Float3.Cross(vertex1 - vertex0, vertex2 - vertex0).Normalized, materialIndex
	) { }

	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2, in Float3 normal, MaterialIndex materialIndex) : this
	(
		vertex0, vertex1, vertex2,
		normal, normal, normal, materialIndex
	) { }

	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
							Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, MaterialIndex materialIndex) : this
	(
		vertex0, vertex1, vertex2,
		Float3.Cross(vertex1 - vertex0, vertex2 - vertex0).Normalized,
		texcoord0, texcoord1, texcoord2, materialIndex
	) { }

	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
							in Float3 normal,
							Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, MaterialIndex materialIndex) : this
	(
		vertex0, vertex1, vertex2,
		normal, normal, normal,
		texcoord0, texcoord1, texcoord2, materialIndex
	) { }

	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
							in Float3 shadingNormal0, in Float3 shadingNormal1, in Float3 shadingNormal2, MaterialIndex materialIndex) : this
	(
		vertex0, vertex1, vertex2,
		shadingNormal0, shadingNormal1, shadingNormal2,
		Float2.Zero, Float2.Zero, Float2.Zero, materialIndex
	) { }

	public PreparedTriangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
							in Float3 shadingNormal0, in Float3 shadingNormal1, in Float3 shadingNormal2,
							Float2 texcoord0, Float2 texcoord1, Float2 texcoord2, MaterialIndex material)
	{
		Ensure.AreEqual(shadingNormal0.SquaredMagnitude, 1f);
		Ensure.AreEqual(shadingNormal1.SquaredMagnitude, 1f);
		Ensure.AreEqual(shadingNormal2.SquaredMagnitude, 1f);

		this.vertex0 = vertex0;
		edge1 = vertex1 - vertex0;
		edge2 = vertex2 - vertex0;

		this.shadingNormal0 = shadingNormal0;
		this.shadingNormal1 = shadingNormal1;
		this.shadingNormal2 = shadingNormal2;

		this.texcoord0 = texcoord0;
		this.texcoord1 = texcoord1;
		this.texcoord2 = texcoord2;

		Material = material;
	}

	public readonly Float3 vertex0;
	public readonly Float3 edge1;
	public readonly Float3 edge2;

	public readonly Float3 shadingNormal0;
	public readonly Float3 shadingNormal1;
	public readonly Float3 shadingNormal2;

	public readonly Float2 texcoord0;
	public readonly Float2 texcoord1;
	public readonly Float2 texcoord2;

	public Float3 Vertex1 => vertex0 + edge1;
	public Float3 Vertex2 => vertex0 + edge2;

	/// <summary>
	/// The geometric normal of this <see cref="TriangleEntity"/>; calculated from <see cref="edge1"/> and <see cref="edge2"/>.
	/// </summary>
	public Float3 Normal => Float3.Cross(edge1, edge2).Normalized;

	/// <inheritdoc/>
	public MaterialIndex Material { get; }

	/// <inheritdoc/>
	public BoxBound BoxBound => new(stackalloc[] { vertex0, Vertex1, Vertex2 });

	/// <inheritdoc/>
	public ConeBound ConeBound => ConeBound.CreateDirection(Normal);

	/// <inheritdoc/>
	public float Area => Float3.Cross(edge1, edge2).Magnitude / 2f;

	/// <summary>
	/// Returns the distance of intersection between this <see cref="PreparedTriangle"/> and <paramref name="ray"/> without
	/// backface culling. If the intersection exists, the distance is returned and <paramref name="uv"/> will contain the
	/// barycentric coordinate of the intersection, otherwise, <see cref="float.PositiveInfinity"/> is returned.
	/// The famous Möller–Trumbore algorithm: https://cadxfem.org/inf/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf
	/// </summary>
	[SkipLocalsInit]
	public float Intersect(in Ray ray, out Float2 uv) => IntersectImpl(ray.origin, ray.direction, out uv);

	/// <summary>
	/// Returns whether <paramref name="ray"/> will intersect with this <see cref="PreparedTriangle"/> after <paramref name="travel"/>.
	/// The famous Möller–Trumbore algorithm: https://cadxfem.org/inf/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf
	/// </summary>
	public bool Intersect(in Ray ray, float travel) => IntersectImpl(ray.origin, ray.direction, travel);

	/// <inheritdoc/>
	public Probable<GeometryPoint> Sample(in Float3 origin, Sample2D sample)
	{
		Float2 uv = sample.UniformTriangle;
		Float3 position = GetPoint(uv);
		Float3 normal = GetShadingNormal(uv);

		GeometryPoint point = new GeometryPoint(position, normal);
		return (point, point.ProbabilityDensity(origin, Area));
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in Float3 origin, in Float3 incident)
	{
		float distance = IntersectImpl(origin, incident, out Float2 uv);

		Ensure.AreEqual(incident.SquaredMagnitude, 1f);
		if (float.IsPositiveInfinity(distance)) return 0f;

		return distance * distance / FastMath.Abs(GetShadingNormal(uv).Dot(incident) * Area);
	}

	public Float3 GetShadingNormal(Float2 uv) => ((1f - uv.X - uv.Y) * shadingNormal0 + uv.X * shadingNormal1 + uv.Y * shadingNormal2).Normalized;
	public Float2 GetTexcoord(Float2 uv) => (1f - uv.X - uv.Y) * texcoord0 + uv.X * texcoord1 + uv.Y * texcoord2;

	public void GetSubdivided(Span<PreparedTriangle> triangles, int iteration)
	{
		int requiredLength = 1 << (iteration * 2);

		if (triangles.Length == requiredLength)
		{
			triangles[0] = this;
			GetSubdivided(triangles, shadingNormal0, shadingNormal1, shadingNormal2);
		}
		else throw ExceptionHelper.Invalid(nameof(triangles), triangles.Length, $"is not long enough! Need at least {requiredLength}!");
	}

	public override string ToString() => $"<{nameof(vertex0)}: {vertex0}, {nameof(Vertex1)}: {Vertex1}, {nameof(Vertex2)}: {Vertex2}>";

	float IntersectImpl(in Float3 origin, in Float3 direction, out Float2 uv)
	{
		const float Infinity = float.PositiveInfinity;
		Unsafe.SkipInit(out uv);

		ref float u = ref Unsafe.As<Float2, float>(ref uv);
		ref float v = ref Unsafe.Add(ref u, 1);

		//Calculate determinant and u
		Float3 cross2 = Float3.Cross(direction, edge2);
		float determinant = Float3.Dot(edge1, cross2);

		//If determinant is close to zero, ray is parallel to triangle
		if (determinant == 0f) return Infinity;
		float determinantR = 1f / determinant;

		Float3 offset = origin - vertex0;
		u = offset.Dot(cross2) * determinantR;

		//Check if is outside barycentric bounds
		if ((u < 0f) | (u > 1f)) return Infinity;

		Float3 cross1 = Float3.Cross(offset, edge1);
		v = direction.Dot(cross1) * determinantR;

		//Check if is outside barycentric bounds
		if ((v < 0f) | (u + v > 1f)) return Infinity;

		//Check if ray is pointing away from triangle
		float distance = edge2.Dot(cross1) * determinantR;
		return distance < 0f ? Infinity : distance;
	}

	bool IntersectImpl(in Float3 origin, in Float3 direction, float travel)
	{
		//Calculate determinant and u
		Float3 cross2 = Float3.Cross(direction, edge2);
		float determinant = Float3.Dot(edge1, cross2);

		//If determinant is close to zero, ray is parallel to triangle
		if (determinant == 0f) return false;
		float sign = MathF.Sign(determinant);
		determinant *= sign;

		Float3 offset = origin - vertex0;
		float u = offset.Dot(cross2) * sign;

		//Check if is outside barycentric bounds
		if ((u < 0f) | (u > determinant)) return false;

		Float3 cross1 = Float3.Cross(offset, edge1);
		float v = direction.Dot(cross1) * sign;

		//Check if is outside barycentric bounds
		if ((v < 0f) | (u + v > determinant)) return false;

		//Check if ray is pointing away from triangle
		float distance = edge2.Dot(cross1) * sign;
		return (distance >= 0f) & (distance < travel * determinant);
	}

	Float3 GetPoint(Float2 uv) => vertex0 + uv.X * edge1 + uv.Y * edge2;

	static void GetSubdivided(Span<PreparedTriangle> triangles, in Float3 normal00, in Float3 normal11, in Float3 normal22)
	{
		if (triangles.Length <= 1) return;

		//The uv locations right in the middle of two vertices
		Float2 uv01 = new Float2(0.5f, 0f);
		Float2 uv02 = new Float2(0f, 0.5f);
		Float2 uv12 = new Float2(0.5f, 0.5f);

		//Begin dividing triangle
		ref readonly PreparedTriangle triangle = ref triangles[0];

		Float3 normal01 = GetInterpolatedNormal(uv01, normal00, normal11, normal22);
		Float3 normal02 = GetInterpolatedNormal(uv02, normal00, normal11, normal22);
		Float3 normal12 = GetInterpolatedNormal(uv12, normal00, normal11, normal22);

		Float3 vertex01 = triangle.GetPoint(uv01);
		Float3 vertex02 = triangle.GetPoint(uv02);
		Float3 vertex12 = triangle.GetPoint(uv12);

		Float3 vertex00 = triangle.vertex0;
		Float3 vertex11 = triangle.Vertex1;
		Float3 vertex22 = triangle.Vertex2;

		Float2 texcoord01 = triangle.GetTexcoord(uv01);
		Float2 texcoord02 = triangle.GetTexcoord(uv02);
		Float2 texcoord12 = triangle.GetTexcoord(uv12);

		Float2 texcoord00 = triangle.texcoord0;
		Float2 texcoord11 = triangle.texcoord1;
		Float2 texcoord22 = triangle.texcoord2;

		Fill(triangles, 0, triangle.Material, vertex01, vertex12, vertex02, normal01, normal12, normal02, texcoord01, texcoord12, texcoord02);
		Fill(triangles, 1, triangle.Material, vertex00, vertex01, vertex02, normal00, normal01, normal02, texcoord00, texcoord01, texcoord02);
		Fill(triangles, 2, triangle.Material, vertex11, vertex12, vertex01, normal11, normal12, normal01, texcoord11, texcoord12, texcoord01);
		Fill(triangles, 3, triangle.Material, vertex22, vertex02, vertex12, normal22, normal02, normal12, texcoord22, texcoord02, texcoord12);

		//NOTE: this normal is not normalized, because normalized normals will mess up during subdivision
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Float3 GetInterpolatedNormal(Float2 uv, in Float3 normal00, in Float3 normal11, in Float3 normal22) => (1f - uv.X - uv.Y) * normal00 + uv.X * normal11 + uv.Y * normal22;

		static void Fill(Span<PreparedTriangle> span, int index, MaterialIndex material,
						 in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
						 in Float3 normal0, in Float3 normal1, in Float3 normal2,
						 Float2 texcoord0, Float2 texcoord1, Float2 texcoord2)
		{
			int gap = span.Length / 4;
			var slice = span.Slice(gap * index, gap);

			slice[0] = new PreparedTriangle
			(
				vertex0, vertex1, vertex2,
				normal0, normal1, normal2,
				texcoord0, texcoord1, texcoord2, material
			);

			GetSubdivided(slice, normal0, normal1, normal2);
		}
	}
}