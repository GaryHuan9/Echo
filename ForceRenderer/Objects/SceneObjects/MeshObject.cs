using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class MeshObject : SceneObject
	{
		public MeshObject(Material material, Mesh mesh) : base(material) => Mesh = mesh;

		public Mesh Mesh { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(int materialToken)
		{
			if (Mesh == null) yield break;

			bool hasTexcoord = Mesh.TexcoordCount > 0;
			bool hasNormal = Mesh.NormalCount > 0;

			for (int i = 0; i < Mesh.TriangleCount; i++)
			{
				Triangle triangle = Mesh.GetTriangle(i);

				if (!hasNormal) yield return new PressedTriangle(GetVertex(0), GetVertex(1), GetVertex(2), materialToken);
				else yield return new PressedTriangle(GetVertex(0), GetVertex(1), GetVertex(2), GetNormal(0), GetNormal(1), GetNormal(2), materialToken);

				Float3 GetVertex(int index)
				{
					Float3 vertex = Mesh.GetVertex(triangle.vertexIndices[index]);
					return LocalToWorld.MultiplyPoint(vertex);
				}

				Float3 GetNormal(int index)
				{
					Float3 normal = Mesh.GetNormal(triangle.normalIndices[index]);
					return LocalToWorld.MultiplyDirection(normal);
				}

				Float2 GetTexcoord(int index) => Mesh.GetTexcoord(triangle.texcoordIndices[index]);
			}
		}

		public override IEnumerable<PressedSphere> ExtractSpheres(int materialToken) => Enumerable.Empty<PressedSphere>();
	}
}