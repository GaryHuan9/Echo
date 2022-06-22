using System.Collections.Generic;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public class TriangleEntity : GeometricEntity, IGeometricEntity<PreparedTriangle>
{
	public Float3 Vertex0 { get; set; } = Float3.Zero;
	public Float3 Vertex1 { get; set; } = Float3.Up;
	public Float3 Vertex2 { get; set; } = Float3.One;

	public Float3? Normal0 { get; set; }
	public Float3? Normal1 { get; set; }
	public Float3? Normal2 { get; set; }
	
	uint IGeometricEntity<PreparedTriangle>.Count => 1;

	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		MaterialIndex material = extractor.Register(Material);
		Float4x4 transform = ForwardTransform;

		Float3 normal = Float3.Cross(Vertex1 - Vertex0, Vertex2 - Vertex0);
		Float3 normal0 = Normal0?.Normalized ?? normal;
		Float3 normal1 = Normal1?.Normalized ?? normal;
		Float3 normal2 = Normal2?.Normalized ?? normal;

		yield return new PreparedTriangle
		(
			transform.MultiplyPoint(Vertex0), transform.MultiplyPoint(Vertex1), transform.MultiplyPoint(Vertex2),
			transform.MultiplyDirection(normal0), transform.MultiplyDirection(normal1), transform.MultiplyDirection(normal2),
			material
		);
	}
}