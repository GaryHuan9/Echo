using System.Collections.Generic;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A geometric mesh object represented by a bunch of triangles.
/// </summary>
public class MeshEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	/// <summary>
	/// The source of the triangles.
	/// </summary>
	public ITriangleSource Source { get; set; }

	/// <inheritdoc/>
	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		if (Source == null) yield break;

		using ITriangleStream stream = Source.CreateStream();
		MaterialIndex material = extractor.Register(Material);
		Float4x4 transform = InverseTransform;

		while (stream.ReadTriangle(out ITriangleStream.Triangle triangle))
		{
			if (triangle.HasNormal)
			{
				yield return new PreparedTriangle
				(
					transform.MultiplyPoint(triangle.vertex0),
					transform.MultiplyPoint(triangle.vertex1),
					transform.MultiplyPoint(triangle.vertex2),
					transform.MultiplyDirection(triangle.normal0).Normalized,
					transform.MultiplyDirection(triangle.normal1).Normalized,
					transform.MultiplyDirection(triangle.normal2).Normalized,
					triangle.texcoord0, triangle.texcoord1, triangle.texcoord2,
					material
				);
			}
			else
			{
				yield return new PreparedTriangle
				(
					transform.MultiplyPoint(triangle.vertex0),
					transform.MultiplyPoint(triangle.vertex1),
					transform.MultiplyPoint(triangle.vertex2),
					triangle.texcoord0, triangle.texcoord1, triangle.texcoord2,
					material
				);
			}
		}
	}
}