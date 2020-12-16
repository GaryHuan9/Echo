using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;

namespace ForceRenderer.Objects.SceneObjects
{
	public class MeshObject : SceneObject
	{
		public MeshObject(Mesh mesh) : base(mesh.materialLibrary[0]) => Mesh = mesh;

		public Mesh Mesh { get; set; }

		public override IEnumerable<PressedTriangle> ExtractTriangles(Func<Material, int> materialConverter)
		{
			if (Mesh == null) yield break;

			for (int i = 0; i < Mesh.TriangleCount; i++)
			{
				Triangle triangle = Mesh.GetTriangle(i);
				Material material = Mesh.materialLibrary[triangle.materialIndex];
				int materialToken = materialConverter(material);

				bool hasNormal = triangle.normalIndices.MinComponent >= 0;
				bool hasTexcoord = triangle.texcoordIndices.MinComponent >= 0;

				if (hasNormal)
				{
					if (hasTexcoord)
					{
						yield return new PressedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2),
							GetNormal(0), GetNormal(1), GetNormal(2),
							GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), materialToken
						);
					}
					else
					{
						yield return new PressedTriangle
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
						yield return new PressedTriangle
						(
							GetVertex(0), GetVertex(1), GetVertex(2),
							GetTexcoord(0), GetTexcoord(1), GetTexcoord(2), materialToken
						);
					}
					else
					{
						yield return new PressedTriangle
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

		public override IEnumerable<PressedSphere> ExtractSpheres(Func<Material, int> materialConverter) => Enumerable.Empty<PressedSphere>();
	}
}