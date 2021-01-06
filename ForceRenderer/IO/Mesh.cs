using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.ObjectPooling;

namespace ForceRenderer.IO
{
	public class Mesh : ILoadableAsset
	{
		public Mesh(string path) //Loads .obj based on http://paulbourke.net/dataformats/obj/
		{
			path = this.GetAbsolutePath(path);
			string currentMaterialName = null;

			foreach (string line in File.ReadAllLines(path))
			{
				string[] parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;

				switch (parts[0])
				{
					case "mtllib":
					{
						materialLibrary = new MaterialTemplateLibrary(this.GetSiblingPath(path, this.GetRemain(parts, 1)));
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
						currentMaterialName = this.GetRemain(parts, 1);
						break;
					}
					case "f":
					{
						//Each face part consists of vertex index, texture coordinate index, and normal index
						//.obj uses counter-clockwise winding order while we use clockwise. So we have to reverse it

						List<string[]> numbers = CollectionPooler<string[]>.list.GetObject();
						for (int i = 1; i < parts.Length; i++) numbers.Add(parts[i].Split('/'));

						if (numbers.Count < 3) throw new Exception($"Invalid 2 face data indices at {path}.");
						if (numbers.Count > 4) DebugHelper.Log($"More than 4 face data index currently not supported at {path}.");

						triangles.Add
						(
							new Triangle
							(
								new Int3(Parse(0, 2), Parse(0, 1), Parse(0, 0)),
								new Int3(Parse(2, 2), Parse(2, 1), Parse(2, 0)),
								new Int3(Parse(1, 2), Parse(1, 1), Parse(1, 0)),
								materialLibrary[currentMaterialName]
							)
						);

						if (numbers.Count == 4)
						{
							triangles.Add
							(
								new Triangle
								(
									new Int3(Parse(0, 0), Parse(0, 3), Parse(0, 2)),
									new Int3(Parse(2, 0), Parse(2, 3), Parse(2, 2)),
									new Int3(Parse(1, 0), Parse(1, 3), Parse(1, 2)),
									materialLibrary[currentMaterialName]
								)
							);
						}

						break;

						int Parse(int dataType, int index)
						{
							string value = numbers[index].TryGetValue(dataType);
							if (!int.TryParse(value, out int result)) return -1;

							ICollection collection = dataType switch
													 {
														 0 => vertices,
														 2 => normals,
														 1 => texcoords,
														 _ => throw ExceptionHelper.Invalid(nameof(dataType), dataType, InvalidType.unexpected)
													 };
							return result < 0 ? collection.Count + result : result - 1;
						}
					}
				}
			}

			vertices.TrimExcess();
			normals.TrimExcess();
			texcoords.TrimExcess();
			triangles.TrimExcess();
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".obj"});
		IReadOnlyList<string> ILoadableAsset.AcceptableFileExtensions => _acceptableFileExtensions;

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