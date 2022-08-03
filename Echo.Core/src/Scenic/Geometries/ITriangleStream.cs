using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Scenic.Geometries;

public interface ITriangleStream : IDisposable
{
	bool ReadTriangle(out Triangle triangle);

	/// <remarks>Winding order is clockwise.</remarks>
	public readonly struct Triangle
	{
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

		public readonly Float3 vertex0;
		public readonly Float3 vertex1;
		public readonly Float3 vertex2;

		public readonly Float3 normal0;
		public readonly Float3 normal1;
		public readonly Float3 normal2;

		public readonly Float2 texcoord0;
		public readonly Float2 texcoord1;
		public readonly Float2 texcoord2;

		public bool HasNormal => normal0 != Float3.Zero;
	}
}