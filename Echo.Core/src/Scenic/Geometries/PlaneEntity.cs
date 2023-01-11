using System.Collections.Generic;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A geometric rectangular plane.
/// </summary>
[EchoSourceUsable]
public class PlaneEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	/// <summary>
	/// The size of the plane.
	/// </summary>
	[EchoSourceUsable]
	public Float2 Size { get; set; } = Float2.One;

	/// <summary>
	/// The texture coordinate of the bottom left of the plane.
	/// </summary>
	[EchoSourceUsable]
	public Float2 Texcoord00 { get; set; } = Float2.Zero;

	/// <summary>
	/// The texture coordinate of the bottom right of the plane.
	/// </summary>
	[EchoSourceUsable]
	public Float2 Texcoord01 { get; set; } = Float2.Right;

	/// <summary>
	/// The texture coordinate of the top left of the plane.
	/// </summary>
	[EchoSourceUsable]
	public Float2 Texcoord10 { get; set; } = Float2.Up;

	/// <summary>
	/// The texture coordinate of the top right of the plane.
	/// </summary>
	[EchoSourceUsable]
	public Float2 Texcoord11 { get; set; } = Float2.One;

	/// <inheritdoc/>
	uint IGeometrySource<PreparedTriangle>.Count => 2;

	/// <inheritdoc/>
	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		Float2 extend = Size / 2f;
		Float4x4 transform = InverseTransform;
		MaterialIndex material = extractor.Register(Material, 2);

		Float3 point00 = transform.MultiplyPoint(new Float3(-extend.X, 0f, -extend.Y));
		Float3 point01 = transform.MultiplyPoint(new Float3(-extend.X, 0f, extend.Y));
		Float3 point10 = transform.MultiplyPoint(new Float3(extend.X, 0f, -extend.Y));
		Float3 point11 = transform.MultiplyPoint(new Float3(extend.X, 0f, extend.Y));

		yield return new PreparedTriangle(point00, point11, point10, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(point00, point01, point11, Texcoord00, Texcoord01, Texcoord11, material);
	}
}