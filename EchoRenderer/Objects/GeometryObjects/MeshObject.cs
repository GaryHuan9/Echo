using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.GeometryObjects
{
	public class MeshObject : GeometryObject
	{
		public MeshObject(Mesh mesh, Material material) : base(material) => Mesh = mesh;

		public MeshObject(Mesh mesh, MaterialLibrary materialLibrary) : this(mesh, materialLibrary.first) => MaterialLibrary = materialLibrary;

		public Mesh Mesh { get; set; }
		public MaterialLibrary MaterialLibrary { get; set; }

		public override IEnumerable<PreparedTriangle> ExtractTriangles(MaterialPreparer preparer)
		{
			if (Mesh == null) yield break;

			for (int i = 0; i < Mesh.TriangleCount; i++)
			{
				Triangle triangle = Mesh.GetTriangle(i);
				Material material = MaterialLibrary?[triangle.materialName] ?? Material;
				int materialToken = preparer.GetToken(material);

				bool hasNormal = triangle.normalIndices.MinComponent >= 0;
				bool hasTexcoord = triangle.texcoordIndices.MinComponent >= 0;

				if (hasNormal)
				{
					if (hasTexcoord)
					{
						yield return new PreparedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2),
							GetNormal(0), GetNormal(1), GetNormal(2),
							GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), materialToken
						);
					}
					else
					{
						yield return new PreparedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2),
							GetNormal(0), GetNormal(1), GetNormal(2), materialToken
						);
					}
				}
				else
				{
					if (hasTexcoord)
					{
						yield return new PreparedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2),
							GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), materialToken
						);
					}
					else
					{
						yield return new PreparedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2), materialToken
						);
					}
				}

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

		public override IEnumerable<PreparedSphere> ExtractSpheres(MaterialPreparer preparer) => Enumerable.Empty<PreparedSphere>();
	}
}