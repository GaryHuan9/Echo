using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

			var usemtlLines = new List<Line>();
			var mtllibLines = new List<Line>();

			var vertexLines = new List<Line>();
			var normalLines = new List<Line>();
			var texcoordLines = new List<Line>();

			var faceLines = new List<Line>();

			int height = 0;

			PerformanceTest test0 = new PerformanceTest();
			PerformanceTest test1 = new PerformanceTest();
			PerformanceTest test2 = new PerformanceTest();

			using (test0.Start())
			{
				//First read all lines to split them into categories
				using StreamReader reader = new StreamReader(File.OpenRead(path));

				while (true)
				{
					string line = reader.ReadLine();
					if (line == null) break;

					ReadOnlySpan<char> span = ((ReadOnlySpan<char>)line).TrimStart();

					int index = span.IndexOf(' ');
					if (index < 0) continue;

					(span[0] switch
							{
								'v' => index switch
								{
									1 => vertexLines,
									2 => span[1] switch
									{
										'n' => normalLines,
										't' => texcoordLines,
										_ => null
									},
									_ => null
								},
								'f' when index == 1 => faceLines,
								'u' when span.StartsWith("usemtl ") => usemtlLines,
								'm' when span.StartsWith("mtllib ") => mtllibLines,
								_ => null
							})?.Add(new Line(line, height++, Range.StartAt(index + 1)));
				}
			}

			//Load material template library
			if (mtllibLines.Count == 1)
			{
				string siblingPath = this.GetSiblingPath(path, mtllibLines[0]);
				materialTemplateLibrary = new MaterialTemplateLibrary(siblingPath);
			}
			else throw new Exception($"Invalid OBJ file at {path} because it does not have exactly one ({mtllibLines.Count}) material template library.");

			//Structure usemtl usages
			using var heightHandles = CollectionPooler<int>.list.Fetch();
			using var indexHandles = CollectionPooler<int>.list.Fetch();

			List<int> materialHeights = heightHandles.Target;
			List<int> materialIndices = indexHandles.Target;

			if (usemtlLines.Count > 0)
			{
				//Construct a sorted divisions list to find the material in log n time

				foreach (Line line in usemtlLines)
				{
					materialHeights.Add(line.height);
					materialIndices.Add(materialTemplateLibrary[line]);
				}
			}
			else throw new Exception($"Invalid OBJ file at {path} because it has zero usemtl usage, meaning it does not use any material.");

			using (test1.Start())
			{
				//Load supporting attributes (vertex, normal, texcoord)
				vertices = new Float3[vertexLines.Count];
				normals = new Float3[normalLines.Count];
				texcoords = new Float2[texcoordLines.Count];

				Parallel.For(0, vertexLines.Count, LoadVertex);
				Parallel.For(0, normalLines.Count, LoadNormal);
				Parallel.For(0, texcoordLines.Count, LoadTexcoord);
			}

			void LoadVertex(int index)
			{
				ReadOnlySpan<char> line = vertexLines[index];
				Span<Range> ranges = stackalloc Range[3];

				Split(line, ' ', ranges);

				//Because .obj files are in a right-handed coordinate system while we have a left-handed coordinate system,
				//We have to simply negate the x axis to convert all of our vertices into the correct space
				vertices[index] = new Float3(-float.Parse(line[ranges[0]]), float.Parse(line[ranges[1]]), float.Parse(line[ranges[2]]));
			}

			void LoadNormal(int index)
			{
				ReadOnlySpan<char> line = normalLines[index];
				Span<Range> ranges = stackalloc Range[3];

				Split(line, ' ', ranges);

				//Same reason as vertices need to negate the x axis, we also have to negate the y axis
				normals[index] = new Float3(-float.Parse(line[ranges[0]]), float.Parse(line[ranges[1]]), float.Parse(line[ranges[2]]));
			}

			void LoadTexcoord(int index)
			{
				ReadOnlySpan<char> line = texcoordLines[index];
				Span<Range> ranges = stackalloc Range[2];

				Split(line, ' ', ranges);

				texcoords[index] = new Float2(float.Parse(line[ranges[0]]), float.Parse(line[ranges[1]]));
			}

			using (test2.Start())
			{
				//Load triangles/faces
				triangles = new List<Triangle>();
				object locker = new object();

				Parallel.For(0, faceLines.Count, LoadFace);
				lock (locker) triangles.TrimExcess();

				void LoadFace(int index)
				{
					Line line = faceLines[index];
					ReadOnlySpan<char> span = line;
					Span<Range> ranges = stackalloc Range[4];

					Split(span, ' ', ranges);

					ReadOnlySpan<char> split0 = span[ranges[0]];
					ReadOnlySpan<char> split1 = span[ranges[1]];
					ReadOnlySpan<char> split2 = span[ranges[2]];
					ReadOnlySpan<char> split3 = span[ranges[3]];

					Span<Range> ranges0 = stackalloc Range[3];
					Span<Range> ranges1 = stackalloc Range[3];
					Span<Range> ranges2 = stackalloc Range[3];

					Split(split0, '/', ranges0);
					Split(split1, '/', ranges1);
					Split(split2, '/', ranges2);

					Int3 indices0 = Parse(split0, ranges0);
					Int3 indices1 = Parse(split1, ranges1);
					Int3 indices2 = Parse(split2, ranges2);

					int heightIndex = ~materialHeights.BinarySearch(line.height) - 1;
					if (heightIndex < 0) throw new Exception("Assigning faces before using materials!");

					int materialIndex = materialIndices[heightIndex];

					//Each face part consists of vertex index, texture coordinate index, and normal index
					//.obj uses counter-clockwise winding order while we use clockwise. So we have to reverse it

					Triangle triangle0 = new Triangle
					(
						new Int3(indices2[0], indices1[0], indices0[0]),
						new Int3(indices2[2], indices1[2], indices0[2]),
						new Int3(indices2[1], indices1[1], indices0[1]),
						materialIndex
					);

					lock (locker) triangles.Add(triangle0);

					if (split3.Length > 0) //We also support 4-vertex face aka quad
					{
						Span<Range> ranges3 = stackalloc Range[3];
						Split(split3, '/', ranges3);
						Int3 indices3 = Parse(split3, ranges3);

						Triangle triangle1 = new Triangle
						(
							new Int3(indices0[0], indices3[0], indices2[0]),
							new Int3(indices0[2], indices3[2], indices2[2]),
							new Int3(indices0[1], indices3[1], indices2[1]),
							materialIndex
						);

						lock (locker) triangles.Add(triangle1);
					}

					static Int3 Parse(ReadOnlySpan<char> span, ReadOnlySpan<Range> ranges)
					{
						Int3 result = default;

						for (int i = 0; i < 3; i++)
						{
							int index = int.TryParse(span[ranges[i]], out int value) ? value : int.MaxValue;
							if (index < 0) throw new Exception("Do not support OBJ negative indices!");

							result = result.Replace(i, index == int.MaxValue ? -1 : index);
						}

						return result;
					}
				}
			}

			Console.WriteLine(test0.ElapsedMilliseconds);
			Console.WriteLine(test1.ElapsedMilliseconds);
			Console.WriteLine(test2.ElapsedMilliseconds);
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".obj"});
		IReadOnlyList<string> ILoadableAsset.AcceptableFileExtensions => _acceptableFileExtensions;

		public readonly MaterialTemplateLibrary materialTemplateLibrary;

		readonly List<Triangle> triangles;

		readonly Float3[] vertices;
		readonly Float3[] normals;
		readonly Float2[] texcoords;

		public int TriangleCount => triangles.Count;

		public Triangle GetTriangle(int index)
		{
			if (triangles.IsIndexValid(index)) return triangles[index];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}

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

		public static void Split(ReadOnlySpan<char> span, char split, Span<Range> ranges)
		{
			int index = 0;

			for (int i = 0; i < ranges.Length; i++)
			{
				int start = index;
				bool comment = false;

				for (; index < span.Length; index++)
				{
					char current = span[index];
					comment = current == '#';

					if (comment || current == split) break;
				}

				ranges[i] = index - start > 0 ? start..index : default;
				if (comment) index = span.Length; //Ignore content behind # as comments

				index++;
			}
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

	readonly struct Line
	{
		public Line(string source, int height, Range range)
		{
			this.height = height;
			this.source = source;
			this.range = range;
		}

		public readonly string source;
		public readonly int height;
		public readonly Range range;

		public static implicit operator ReadOnlySpan<char>(Line line) => ((ReadOnlySpan<char>)line.source)[line.range];
		public static implicit operator string(Line line) => line.source[line.range];
	}
}