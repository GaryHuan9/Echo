using System.Collections.Generic;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A geometric mesh object represented by a bunch of triangles.
/// </summary>
[EchoSourceUsable]
public class MeshEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	/// <summary>
	/// The source of the triangles.
	/// </summary>
	[EchoSourceUsable]
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
			yield return triangle.Prepare(transform, material);
		}
	}
}