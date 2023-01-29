using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A series of triangle geometries that can be retrieved one after another.
/// </summary>
public interface ITriangleStream : IDisposable
{
	/// <summary>
	/// Reads the next <see cref="Triangle"/> in the <see cref="ITriangleStream"/>.
	/// </summary>
	/// <param name="triangle">If available, outputs the <see cref="Triangle"/> that was read.</param>
	/// <returns>Whether a new <see cref="Triangle"/> was successfully read. False
	/// means we have reached the end of this <see cref="ITriangleStream"/>.</returns>
	bool ReadTriangle(out Triangle triangle);

	/// <summary>
	/// A struct containing data necessary to create a <see cref="PreparedTriangle"/>.
	/// </summary>
	public readonly struct Triangle
	{
		/// <summary>
		/// Constructs a <see cref="Triangle"/> with only vertex data.
		/// </summary>
		/// <remarks>Winding order is clockwise.</remarks>
		public Triangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2)
		{
			this.vertex0 = vertex0;
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;

			normal0 = Float3.Zero;
			normal1 = Float3.Zero;
			normal2 = Float3.Zero;

			texcoord0 = Float2.Zero;
			texcoord1 = Float2.Zero;
			texcoord2 = Float2.Zero;
		}

		/// <summary>
		/// Constructs a <see cref="Triangle"/> with vertex and normal data.
		/// </summary>
		/// <remarks>Winding order is clockwise.</remarks>
		public Triangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
						in Float3 normal0, in Float3 normal1, in Float3 normal2)
		{
			Ensure.AreNotEqual(normal0.SquaredMagnitude, 0f);
			Ensure.AreNotEqual(normal1.SquaredMagnitude, 0f);
			Ensure.AreNotEqual(normal2.SquaredMagnitude, 0f);

			this.vertex0 = vertex0;
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;

			this.normal0 = normal0;
			this.normal1 = normal1;
			this.normal2 = normal2;

			texcoord0 = Float2.Zero;
			texcoord1 = Float2.Zero;
			texcoord2 = Float2.Zero;
		}

		/// <summary>
		/// Constructs a <see cref="Triangle"/> with vertex and texture coordinate data.
		/// </summary>
		/// <remarks>Winding order is clockwise.</remarks>
		public Triangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
						Float2 texcoord0, Float2 texcoord1, Float2 texcoord2)
		{
			this.vertex0 = vertex0;
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;

			normal0 = Float3.Zero;
			normal1 = Float3.Zero;
			normal2 = Float3.Zero;

			this.texcoord0 = texcoord0;
			this.texcoord1 = texcoord1;
			this.texcoord2 = texcoord2;
		}

		/// <summary>
		/// Constructs a <see cref="Triangle"/> with vertex, normal, and texture coordinate data.
		/// </summary>
		/// <remarks>Winding order is clockwise.</remarks>
		public Triangle(in Float3 vertex0, in Float3 vertex1, in Float3 vertex2,
						in Float3 normal0, in Float3 normal1, in Float3 normal2,
						Float2 texcoord0, Float2 texcoord1, Float2 texcoord2)
		{
			Ensure.AreNotEqual(normal0.SquaredMagnitude, 0f);
			Ensure.AreNotEqual(normal1.SquaredMagnitude, 0f);
			Ensure.AreNotEqual(normal2.SquaredMagnitude, 0f);

			this.vertex0 = vertex0;
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;

			this.normal0 = normal0;
			this.normal1 = normal1;
			this.normal2 = normal2;

			this.texcoord0 = texcoord0;
			this.texcoord1 = texcoord1;
			this.texcoord2 = texcoord2;
		}

		readonly Float3 vertex0;
		readonly Float3 vertex1;
		readonly Float3 vertex2;

		readonly Float3 normal0;
		readonly Float3 normal1;
		readonly Float3 normal2;

		readonly Float2 texcoord0;
		readonly Float2 texcoord1;
		readonly Float2 texcoord2;

		bool HasNormal => normal0 != Float3.Zero;

		/// <summary>
		/// Creates a <see cref="PreparedTriangle"/> out of this <see cref="Triangle"/>.
		/// </summary>
		/// <param name="transform">The <see cref="Float4x4"/> transform applied during this preparation.</param>
		/// <param name="material">The <see cref="MaterialIndex"/> that represents the <see cref="Material"/>.</param>
		/// <returns>The newly created <see cref="PreparedTriangle"/>.</returns>
		public PreparedTriangle Prepare(in Float4x4 transform, MaterialIndex material) => HasNormal ?
			new PreparedTriangle
			(
				transform.MultiplyPoint(vertex0),
				transform.MultiplyPoint(vertex1),
				transform.MultiplyPoint(vertex2),
				transform.MultiplyDirection(normal0).Normalized,
				transform.MultiplyDirection(normal1).Normalized,
				transform.MultiplyDirection(normal2).Normalized,
				texcoord0, texcoord1, texcoord2,
				material
			) :
			new PreparedTriangle
			(
				transform.MultiplyPoint(vertex0),
				transform.MultiplyPoint(vertex1),
				transform.MultiplyPoint(vertex2),
				texcoord0, texcoord1, texcoord2,
				material
			);
	}
}