using System;
using System.Collections.Generic;
using System.IO;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class Mesh
	{
		public Mesh(string relativePath)
		{
			string extension = Path.GetExtension(relativePath);

			if (string.IsNullOrEmpty(extension)) relativePath = Path.ChangeExtension(relativePath, FileExtension);
			else if (extension != FileExtension) throw ExceptionHelper.Invalid(nameof(relativePath), relativePath, "has invalid extension!");

			string path = AssetsUtility.GetAssetsPath(relativePath);
			if (!File.Exists(path)) throw new FileNotFoundException($"No obj file located at {path}.");

			vertices = new List<Float3>();
			triangles = new List<Int3>();

			foreach (string line in File.ReadAllLines(path))
			{
				string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;

				switch (parts[0])
				{
					case "v":
					{
						vertices.Add(new Float3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
						break;
					}
					case "f":
					{
						string[] numbers0 = parts[1].Split('/');
						string[] numbers1 = parts[2].Split('/');
						string[] numbers2 = parts[3].Split('/');

						//Each face part is consists of vertex index, texture coord index, and normal index. We might use them later
						triangles.Add(new Int3(int.Parse(numbers0[0]) - 1, int.Parse(numbers1[0]) - 1, int.Parse(numbers2[0]) - 1));

						break;
					}
				}
			}

			vertices.TrimExcess();
			triangles.TrimExcess();
		}

		public const string FileExtension = ".obj";

		readonly List<Float3> vertices;
		readonly List<Int3> triangles;

		public int VertexCount => vertices.Count;
		public int TriangleCount => triangles.Count;

		public Float3 GetVertex(int index)
		{
			if (vertices.IsIndexValid(index)) return vertices[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}

		public Int3 GetTriangle(int index)
		{
			if (triangles.IsIndexValid(index)) return triangles[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}
	}
}