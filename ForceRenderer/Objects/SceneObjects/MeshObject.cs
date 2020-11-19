using System;
using System.Collections.Generic;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class MeshObject : SceneObject
	{
		public MeshObject(Material material, Mesh mesh) : base(material) => Mesh = mesh;

		public Mesh Mesh { get; set; }

		public override void Press(List<PressedTriangle> triangles, List<PressedSphere> spheres, int materialToken)
		{
			if (Mesh == null) return;

			triangles.Capacity = Math.Max(triangles.Capacity, triangles.Count + Mesh.TriangleCount);

			for (int i = 0; i < Mesh.TriangleCount; i++)
			{
				Int3 indices = Mesh.GetTriangle(i);
				triangles.Add(new PressedTriangle(GetVertex(0), GetVertex(1), GetVertex(2), materialToken));

				Float3 GetVertex(int index)
				{
					Float3 vertex = Mesh.GetVertex(indices[index]);
					return LocalToWorld.MultiplyPoint(vertex);
				}
			}
		}
	}
}