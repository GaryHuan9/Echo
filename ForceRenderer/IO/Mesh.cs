using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class Mesh : LoadableAsset
	{
		public Mesh(string path) //Loads .obj based on http://paulbourke.net/dataformats/obj/
		{
			path = GetAbsolutePath(path);
			string currentMaterialName = null;

			foreach (string line in File.ReadAllLines(path))
			{
				string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;

				switch (parts[0])
				{
					case "mtllib":
					{
						materialLibrary = new MaterialTemplateLibrary(GetSiblingPath(path, GetRemain(parts, 1)));
						break;
					}
					case "v":
					{
						//Because .obj files are in a right-handed coordinate system while we have a left-handed coordinate system,
						//We have to simply negate the x axis to convert all of our vertices into the correct space
						vertices.Add(new Float3(-float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));

						break;
					}
					case "vn":
					{
						normals.Add(new Float3(-float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
						break;
					}
					case "vt":
					{
						texcoords.Add(new Float2(float.Parse(parts[1]), float.Parse(parts[2])));
						break;
					}
					case "usemtl":
					{
						currentMaterialName = GetRemain(parts, 1);
						break;
					}
					case "f":
					{
						string[] numbers0 = parts[1].Split('/');
						string[] numbers1 = parts[2].Split('/');
						string[] numbers2 = parts[3].Split('/');

						//Each face part consists of vertex index, texture coordinate index, and normal index
						//.obj uses counter-clockwise winding order while we use clockwise. So we have to reverse it

						triangles.Add
						(
							new Triangle
							(
								new Int3(Parse(numbers2.TryGetValue(0)), Parse(numbers1.TryGetValue(0)), Parse(numbers0.TryGetValue(0))),
								new Int3(Parse(numbers2.TryGetValue(2)), Parse(numbers1.TryGetValue(2)), Parse(numbers0.TryGetValue(2))),
								new Int3(Parse(numbers2.TryGetValue(1)), Parse(numbers1.TryGetValue(1)), Parse(numbers0.TryGetValue(1))),
								materialLibrary[currentMaterialName]
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

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".obj"});
		protected override IReadOnlyList<string> AcceptableFileExtensions => _acceptableFileExtensions;

		public readonly MaterialTemplateLibrary materialLibrary;

		readonly List<Float3> vertices = new List<Float3>();
		readonly List<Float3> normals = new List<Float3>();
		readonly List<Float2> texcoords = new List<Float2>();
		readonly List<Triangle> triangles = new List<Triangle>();

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
		public Triangle(Int3 vertexIndices, Int3 normalIndices, Int3 texcoordIndices, int materialIndex)
		{
			this.vertexIndices = vertexIndices;
			this.normalIndices = normalIndices;
			this.texcoordIndices = texcoordIndices;
			this.materialIndex = materialIndex;
		}

		public readonly Int3 vertexIndices;
		public readonly Int3 normalIndices;
		public readonly Int3 texcoordIndices;
		public readonly int materialIndex;
	}
}