using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Packed;
using EchoRenderer.Core.Scenic.Preparation;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.Scenic.Geometries;

public class MeshEntity : GeometryEntity
{
	public Mesh Mesh { get; set; }
	public MaterialLibrary MaterialLibrary { get; set; }

	public override IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor)
	{
		if (Mesh == null) yield break;

		for (int i = 0; i < Mesh.TriangleCount; i++)
		{
			Triangle triangle = Mesh.GetTriangle(i);
			MaterialIndex material = extractor.Register(MaterialLibrary?[triangle.materialName] ?? Material);

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
						GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), material
					);
				}
				else
				{
					yield return new PreparedTriangle
					(
						GetVertex(0), GetVertex(1), GetVertex(2),
						GetNormal(0), GetNormal(1), GetNormal(2), material
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
						GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), material
					);
				}
				else
				{
					yield return new PreparedTriangle
					(
						GetVertex(0), GetVertex(1), GetVertex(2), material
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

	public override IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor) => Enumerable.Empty<PreparedSphere>();
}