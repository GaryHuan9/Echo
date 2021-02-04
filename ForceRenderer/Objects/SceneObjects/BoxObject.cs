using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Objects.SceneObjects
{
	public class BoxObject : SceneObject
	{
		public BoxObject(Material material, Float3 size) : base(material) => Size = size;

		public Float3 Size { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(Func<Material, int> materialConverter)
		{
			Float3 extend = Size / 2f;
			int materialToken = materialConverter(Material);

			Float3 nnn = GetVertex(-1, -1, -1);
			Float3 nnp = GetVertex(-1, -1, 1);
			Float3 npn = GetVertex(-1, 1, -1);
			Float3 npp = GetVertex(-1, 1, 1);

			Float3 pnn = GetVertex(1, -1, -1);
			Float3 pnp = GetVertex(1, -1, 1);
			Float3 ppn = GetVertex(1, 1, -1);
			Float3 ppp = GetVertex(1, 1, 1);

			//X axis
			yield return new PressedTriangle(pnn, ppp, pnp, materialToken);
			yield return new PressedTriangle(pnn, ppn, ppp, materialToken);

			yield return new PressedTriangle(nnp, npn, nnn, materialToken);
			yield return new PressedTriangle(nnp, npp, npn, materialToken);

			//Y axis
			yield return new PressedTriangle(npn, ppp, ppn, materialToken);
			yield return new PressedTriangle(npn, npp, ppp, materialToken);

			yield return new PressedTriangle(pnn, nnp, nnn, materialToken);
			yield return new PressedTriangle(pnn, pnp, nnp, materialToken);

			//Z axis
			yield return new PressedTriangle(pnp, npp, nnp, materialToken);
			yield return new PressedTriangle(pnp, ppp, npp, materialToken);

			yield return new PressedTriangle(nnn, ppn, pnn, materialToken);
			yield return new PressedTriangle(nnn, npn, ppn, materialToken);

			Float3 GetVertex(int x, int y, int z) => LocalToWorld.MultiplyPoint(new Float3(x, y, z) * extend);
		}

		public override IEnumerable<PressedSphere> ExtractSpheres(Func<Material, int> materialConverter) => Enumerable.Empty<PressedSphere>();
	}
}