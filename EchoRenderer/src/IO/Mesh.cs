using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Pooling;
using CodeHelpers.Threads;

namespace EchoRenderer.IO;

public class Mesh
{
	public Mesh(string path) //Loads .obj based on http://paulbourke.net/dataformats/obj/
	{
		path = AssetsUtility.GetAbsolutePath(acceptableFileExtensions, path);

		//These lists will become too large to pool
		var vertexLines = new List<Line>();
		var normalLines = new List<Line>();
		var texcoordLines = new List<Line>();

		var faceLines = new List<Line>();
		var usemtlLines = new List<Line>();

		int height = 0;

		//First read all lines to split them into categories
		Stream stream;

		if (Path.GetExtension(path) == ".obj") stream = File.OpenRead(path);
		else stream = ZipFile.OpenRead(path).Entries[0].Open(); //Unzip zipped obj file

		using StreamReader reader = new StreamReader(stream);

		while (true)
		{
			string line = reader.ReadLine();
			if (line == null) break;

			var span = ((ReadOnlySpan<char>)line).TrimStart();

			int index = span.IndexOf(' ');
			if (index < 0) continue;

			var list = span[0] switch
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
				_ => null
			};

			list?.Add(new Line(line, height++, Range.StartAt(index + 1)));
		}

		//Structure usemtl usages
		using var heightHandles = CollectionPooler<int>.list.Fetch();
		using var nameHandles = CollectionPooler<string>.list.Fetch();

		List<int> materialHeights = heightHandles.Target;
		List<string> materialNames = nameHandles.Target;

		if (usemtlLines.Count > 0)
		{
			//Construct a sorted divisions list to find the material in log n time

			foreach (Line line in usemtlLines)
			{
				materialHeights.Add(line.height);
				materialNames.Add(line);
			}
		}
		else throw new Exception($"Invalid OBJ file at {path} because it has zero usemtl usage, meaning it does not use any material.");

		//Load supporting attributes (vertex, normal, texcoord)
		vertices = new Float3[vertexLines.Count];
		normals = new Float3[normalLines.Count];
		texcoords = new Float2[texcoordLines.Count];

		Parallel.For(0, vertexLines.Count, LoadVertex);
		Parallel.For(0, normalLines.Count, LoadNormal);
		Parallel.For(0, texcoordLines.Count, LoadTexcoord);

		void LoadVertex(int index)
		{
			ReadOnlySpan<char> line = vertexLines[index];
			Span<Range> ranges = stackalloc Range[3];

			Split(line, ' ', ranges);

			//Because .obj files are in a right-handed coordinate system while we have a left-handed coordinate system,
			//We have to simply negate the x axis to convert all of our vertices into the correct space
			vertices[index] = new Float3(-ParseSingle(line[ranges[0]]), ParseSingle(line[ranges[1]]), ParseSingle(line[ranges[2]]));
		}

		void LoadNormal(int index)
		{
			ReadOnlySpan<char> line = normalLines[index];
			Span<Range> ranges = stackalloc Range[3];

			Split(line, ' ', ranges);

			//Same reason as vertices need to negate the x axis, we also have to negate the y axis
			normals[index] = new Float3(-ParseSingle(line[ranges[0]]), ParseSingle(line[ranges[1]]), ParseSingle(line[ranges[2]]));
		}

		void LoadTexcoord(int index)
		{
			ReadOnlySpan<char> line = texcoordLines[index];
			Span<Range> ranges = stackalloc Range[2];

			Split(line, ' ', ranges);

			texcoords[index] = new Float2(ParseSingle(line[ranges[0]]), ParseSingle(line[ranges[1]]));
		}

		//Load triangles/faces
		triangles0 = new Triangle[faceLines.Count];
		triangles1 = new Triangle[faceLines.Count];

		int triangles1Length = 0;

		Parallel.For(0, faceLines.Count, LoadFace);
		Array.Resize(ref triangles1, InterlockedHelper.Read(ref triangles1Length));

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

			Split(split0, '/', ranges0, false);
			Split(split1, '/', ranges1, false);
			Split(split2, '/', ranges2, false);

			Int3 indices0 = ParseIndices(split0, ranges0);
			Int3 indices1 = ParseIndices(split1, ranges1);
			Int3 indices2 = ParseIndices(split2, ranges2);

			int heightIndex = ~materialHeights.BinarySearch(line.height) - 1;
			if (heightIndex < 0) throw new Exception("Assigning faces before using materials!");

			string materialName = materialNames[heightIndex];

			//Each face part consists of vertex index, texture coordinate index, and normal index
			//.obj uses counter-clockwise winding order while we use clockwise. So we have to reverse it

			triangles0[index] = new Triangle
			(
				new Int3(indices2[0], indices1[0], indices0[0]),
				new Int3(indices2[2], indices1[2], indices0[2]),
				new Int3(indices2[1], indices1[1], indices0[1]),
				materialName
			);

			if (split3.Length > 0) //If we need to add an extra triangle to support 4-vertex face aka quad
			{
				Span<Range> ranges3 = stackalloc Range[3];
				Split(split3, '/', ranges3);
				Int3 indices3 = ParseIndices(split3, ranges3);

				triangles1[Interlocked.Increment(ref triangles1Length) - 1] = new Triangle
				(
					new Int3(indices0[0], indices3[0], indices2[0]),
					new Int3(indices0[2], indices3[2], indices2[2]),
					new Int3(indices0[1], indices3[1], indices2[1]),
					materialName
				);
			}
		}
	}

	static readonly ReadOnlyCollection<string> acceptableFileExtensions = new(new[] { ".obj", ".zip" });

	readonly Triangle[] triangles0; //Triangles are stored in two different arrays to support loading quads
	readonly Triangle[] triangles1;

	readonly Float3[] vertices;
	readonly Float3[] normals;
	readonly Float2[] texcoords;

	public int TriangleCount => triangles0.Length + triangles1.Length;

	public Triangle GetTriangle(int index)
	{
		if (triangles0.IsIndexValid(index)) return triangles0[index];
		index -= triangles0.Length;

		if (triangles1.IsIndexValid(index)) return triangles1[index];
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

	static void Split(ReadOnlySpan<char> span, char split, Span<Range> ranges, bool removeEmpties = true)
	{
		int index = 0;

		for (int i = 0; i < ranges.Length; i++)
		{
			while (removeEmpties && index < span.Length && span[index] == split) index++;

			int start = index;
			bool comment = false;

			for (; index < span.Length; index++)
			{
				char current = span[index];
				comment = current == '#';

				if (comment || current == split) break;
			}

			ranges[i] = index - start > 0 ? start..index : default;
			if (comment) break; //Ignore content behind # as comments

			index++;
		}
	}

	static int ParseInt32(ReadOnlySpan<char> span)
	{
		bool isNegative = span[0] == '-';
		int result = 0;

		for (int i = isNegative ? 1 : 0; i < span.Length; i++)
		{
			result = result * 10 + span[i] - '0';
		}

		return isNegative ? -result : result;
	}

	static float ParseSingle(ReadOnlySpan<char> span)
	{
		bool isNegative = span[0] == '-';
		int index = isNegative ? 1 : 0;

		int integer = 0;
		float fraction = 0f;

		for (; index < span.Length; index++)
		{
			char current = span[index];
			if (current == '.') break;

			integer = integer * 10 + current - '0';
		}

		for (int i = span.Length - 1; i > index; i--)
		{
			char current = span[i];
			if (current == '.') break;

			fraction += current - '0';
			fraction /= 10f;
		}

		float result = integer + fraction;
		return isNegative ? -result : result;
	}

	static Int3 ParseIndices(ReadOnlySpan<char> span, ReadOnlySpan<Range> ranges)
	{
		Int3 result = Int3.negativeOne;

		for (int i = 0; i < 3; i++)
		{
			ReadOnlySpan<char> slice = span[ranges[i]];

			if (slice.Length <= 0) continue;
			int index = ParseInt32(slice);

			if (index > 0) result = result.Replace(i, index - 1);
			else throw new Exception("Do not support OBJ non-positive indices!");
		}

		return result;
	}
}

public readonly struct Triangle
{
	public Triangle(Int3 vertexIndices, Int3 normalIndices, Int3 texcoordIndices, string materialName)
	{
		this.vertexIndices = vertexIndices;
		this.normalIndices = normalIndices;
		this.texcoordIndices = texcoordIndices;
		this.materialName = materialName;
	}

	public readonly Int3 vertexIndices;
	public readonly Int3 normalIndices;
	public readonly Int3 texcoordIndices;
	public readonly string materialName;
}