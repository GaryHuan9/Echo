using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.GeometryObjects
{
	public class BoxObject : GeometryObject
	{
		public BoxObject(Material material, Float3 size) : base(material) => Size = size;

		public Float3 Size { get; set; }

		public Float2 Texcoord00 { get; set; } = Float2.zero;
		public Float2 Texcoord01 { get; set; } = Float2.right;
		public Float2 Texcoord10 { get; set; } = Float2.up;
		public Float2 Texcoord11 { get; set; } = Float2.one;

		public override IEnumerable<PressedTriangle> ExtractTriangles(MaterialPresser presser)
		{
			Float3 extend = Size / 2f;
			int materialToken = presser.GetToken(Material);

			Float3 nnn = GetVertex(-1, -1, -1);
			Float3 nnp = GetVertex(-1, -1, 1);
			Float3 npn = GetVertex(-1, 1, -1);
			Float3 npp = GetVertex(-1, 1, 1);

			Float3 pnn = GetVertex(1, -1, -1);
			Float3 pnp = GetVertex(1, -1, 1);
			Float3 ppn = GetVertex(1, 1, -1);
			Float3 ppp = GetVertex(1, 1, 1);

			//X axis
			yield return new PressedTriangle(pnn, ppp, pnp, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(pnn, ppn, ppp, Texcoord00, Texcoord01, Texcoord11, materialToken);

			yield return new PressedTriangle(nnp, npn, nnn, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(nnp, npp, npn, Texcoord00, Texcoord01, Texcoord11, materialToken);

			//Y axis
			yield return new PressedTriangle(npn, ppp, ppn, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(npn, npp, ppp, Texcoord00, Texcoord01, Texcoord11, materialToken);

			yield return new PressedTriangle(pnn, nnp, nnn, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(pnn, pnp, nnp, Texcoord00, Texcoord01, Texcoord11, materialToken);

			//Z axis
			yield return new PressedTriangle(pnp, npp, nnp, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(pnp, ppp, npp, Texcoord00, Texcoord01, Texcoord11, materialToken);

			yield return new PressedTriangle(nnn, ppn, pnn, Texcoord00, Texcoord11, Texcoord10, materialToken);
			yield return new PressedTriangle(nnn, npn, ppn, Texcoord00, Texcoord01, Texcoord11, materialToken);

			Float3 GetVertex(int x, int y, int z) => LocalToWorld.MultiplyPoint(new Float3(x, y, z) * extend);
		}

		public override IEnumerable<PressedSphere> ExtractSpheres(MaterialPresser presser) => Enumerable.Empty<PressedSphere>();
	}
}