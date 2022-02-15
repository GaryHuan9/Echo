using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Scenic.Geometries;

public class BoxEntity : GeometryEntity
{
	public BoxEntity(Material material, Float3 size) : base(material) => Size = size;

	public Float3 Size { get; set; }

	public Float2 Texcoord00 { get; set; } = Float2.zero;
	public Float2 Texcoord01 { get; set; } = Float2.right;
	public Float2 Texcoord10 { get; set; } = Float2.up;
	public Float2 Texcoord11 { get; set; } = Float2.one;

	public override IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor)
	{
		Float3 extend = Size / 2f;
		MaterialIndex material = extractor.Register(Material);

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

		Float3 GetVertex(int x, int y, int z) => LocalToWorld.MultiplyPoint(new Float3(x, y, z) * extend);
	}

	public override IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor) => Enumerable.Empty<PreparedSphere>();
}