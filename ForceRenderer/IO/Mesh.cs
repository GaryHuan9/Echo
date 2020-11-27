using System;
using System.Collections.Generic;
using System.Diagnostics;
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
					case "vn":
					{
						normals.Add(new Float3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
						break;
					}
					case "f":
					{
						string[] numbers0 = parts[1].Split('/');
						string[] numbers1 = parts[2].Split('/');
						string[] numbers2 = parts[3].Split('/');

						//Each face part consists of vertex index, texture coordinate index, and normal index

						triangles.Add
						(
							new Triangle
							(
								new Int3(Parse(numbers0[0]), Parse(numbers1[0]), Parse(numbers2[0])),
								new Int3(Parse(numbers0.TryGetValue(2)), Parse(numbers1.TryGetValue(2)), Parse(numbers2.TryGetValue(2))),
								new Int3(Parse(numbers0.TryGetValue(1)), Parse(numbers1.TryGetValue(1)), Parse(numbers2.TryGetValue(1)))
							)
						);

						break;

						static int Parse(string value) => int.TryParse(value, out int result) ? result - 1 : -1;
					}
				}
			}

			vertices.TrimExcess();
			normals.TrimExcess();
			texcoords.TrimExcess();
			triangles.TrimExcess();
		}

		public const string FileExtension = ".obj";

		readonly List<Float3> vertices = new List<Float3>();
		readonly List<Float3> normals = new List<Float3>();
		readonly List<Float2> texcoords = new List<Float2>();
		readonly List<Triangle> triangles = new List<Triangle>();

		public int VertexCount => vertices.Count;
		public int NormalCount => normals.Count;
		public int TexcoordCount => texcoords.Count;
		public int TriangleCount => triangles.Count;

		public Float3 GetVertex(int index)
		{
			if (vertices.IsIndexValid(index)) return vertices[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}

		public Float3 GetNormal(int index)
		{
			if (normals.IsIndexValid(index)) return normals[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}

		public Float2 GetTexcoord(int index)
		{
			if (texcoords.IsIndexValid(index)) return texcoords[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}

		public Triangle GetTriangle(int index)
		{
			if (triangles.IsIndexValid(index)) return triangles[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}
	}

	public readonly struct Triangle
	{
		public Triangle(Int3 vertexIndices, Int3 normalIndices, Int3 texcoordIndices)
		{
			this.vertexIndices = vertexIndices;
			this.normalIndices = normalIndices;
			this.texcoordIndices = texcoordIndices;
		}

		public readonly Int3 vertexIndices;
		public readonly Int3 normalIndices;
		public readonly Int3 texcoordIndices;
	}
}