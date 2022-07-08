using System.Collections.Generic;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A geometric box object.
/// </summary>
public class BoxEntity : MaterialEntity, IGeometrySource<PreparedTriangle>
{
	/// <summary>
	/// The size of the box.
	/// </summary>
	public Float3 Size { get; set; } = Float3.One;

	/// <summary>
	/// The texture coordinate of the bottom left of the box.
	/// </summary>
	public Float2 Texcoord00 { get; set; } = Float2.Zero;

	/// <summary>
	/// The texture coordinate of the bottom right of the box.
	/// </summary>
	public Float2 Texcoord01 { get; set; } = Float2.Right;

	/// <summary>
	/// The texture coordinate of the top left of the box.
	/// </summary>
	public Float2 Texcoord10 { get; set; } = Float2.Up;

	/// <summary>
	/// The texture coordinate of the top right of the box.
	/// </summary>
	public Float2 Texcoord11 { get; set; } = Float2.One;

	/// <inheritdoc/>
	uint IGeometrySource<PreparedTriangle>.Count => 12;

	/// <inheritdoc/>
	public IEnumerable<PreparedTriangle> Extract(SwatchExtractor extractor)
	{
		Float3 extend = Size / 2f;
		Float4x4 transform = InverseTransform;
		MaterialIndex material = extractor.Register(Material, 12);

		Float3 nnn = GetVertex(-1, -1, -1);
		Float3 nnp = GetVertex(-1, -1, 1);
		Float3 npn = GetVertex(-1, 1, -1);
		Float3 npp = GetVertex(-1, 1, 1);

		Float3 pnn = GetVertex(1, -1, -1);
		Float3 pnp = GetVertex(1, -1, 1);
		Float3 ppn = GetVertex(1, 1, -1);
		Float3 ppp = GetVertex(1, 1, 1);

		//X axis
		yield return new PreparedTriangle(pnn, ppp, pnp, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(pnn, ppn, ppp, Texcoord00, Texcoord01, Texcoord11, material);

		yield return new PreparedTriangle(nnp, npn, nnn, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(nnp, npp, npn, Texcoord00, Texcoord01, Texcoord11, material);

		//Y axis
		yield return new PreparedTriangle(npn, ppp, ppn, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(npn, npp, ppp, Texcoord00, Texcoord01, Texcoord11, material);

		yield return new PreparedTriangle(pnn, nnp, nnn, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(pnn, pnp, nnp, Texcoord00, Texcoord01, Texcoord11, material);

		//Z axis
		yield return new PreparedTriangle(pnp, npp, nnp, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(pnp, ppp, npp, Texcoord00, Texcoord01, Texcoord11, material);

		yield return new PreparedTriangle(nnn, ppn, pnn, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(nnn, npn, ppn, Texcoord00, Texcoord01, Texcoord11, material);

		Float3 GetVertex(int x, int y, int z) => transform.MultiplyPoint(new Float3(x, y, z) * extend);
	}
}