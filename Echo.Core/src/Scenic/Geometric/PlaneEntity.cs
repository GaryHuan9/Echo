using System.Collections.Generic;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public class PlaneEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	public Float2 Size { get; set; } = Float2.One;

	public Float2 Texcoord00 { get; set; } = Float2.Zero;
	public Float2 Texcoord01 { get; set; } = Float2.Right;
	public Float2 Texcoord10 { get; set; } = Float2.Up;
	public Float2 Texcoord11 { get; set; } = Float2.One;

	uint IGeometrySource<PreparedTriangle>.Count => 2;

	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		Float2 extend = Size / 2f;
		Float4x4 transform = ForwardTransform;
		MaterialIndex material = extractor.Register(Material, 2);

		Float3 point00 = transform.MultiplyPoint(new Float3(-extend.X, 0f, -extend.Y));
		Float3 point01 = transform.MultiplyPoint(new Float3(-extend.X, 0f, extend.Y));
		Float3 point10 = transform.MultiplyPoint(new Float3(extend.X, 0f, -extend.Y));
		Float3 point11 = transform.MultiplyPoint(new Float3(extend.X, 0f, extend.Y));

		yield return new PreparedTriangle(point00, point11, point10, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(point00, point01, point11, Texcoord00, Texcoord01, Texcoord11, material);
	}
}