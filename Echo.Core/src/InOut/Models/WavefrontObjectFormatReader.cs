using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Echo.Core.Common.Packed;
using Echo.Core.Common.Threading;
using Echo.Core.Scenic.Geometries;

namespace Echo.Core.InOut.Models;

/// <summary>
/// Reads .obj files based on http://paulbourke.net/dataformats/obj/
/// </summary>
public sealed class WavefrontObjectFormatReader : ITriangleStream
{
	public WavefrontObjectFormatReader(string path)
	{
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

		using var reader = new StreamReader(stream);

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
						_   => null
					},
					_ => null
				},
				'f' when index == 1                 => faceLines,
				'u' when span.StartsWith("usemtl ") => usemtlLines,
				_                                   => null
			};

			list?.Add(new Line(line, height++, Range.StartAt(index + 1)));
		}

		//Structure usemtl usages
		List<int> materialHeights = new();
		List<string> materialNames = new();

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
		packs0 = new IndicesPack[faceLines.Count];
		packs1 = new IndicesPack[faceLines.Count];

		int triangles1Length = 0;

		Parallel.For(0, faceLines.Count, LoadFace);
		Array.Resize(ref packs1, InterlockedHelper.Read(ref triangles1Length));

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

			packs0[index] = new IndicesPack
			(
				new Int3(indices2[0], indices1[0], indices0[0]),
				new Int3(indices2[2], indices1[2], indices0[2]),
				new Int3(indices2[1], indices1[1], indices0[1]),
				materialName
			);

			if (split3.Length > 0) //If we need to add an extra triangle to support 4-vertex face aka quad
			{
				Span<Range> ranges3 = stackalloc Range[3];
				Split(split3, '/', ranges3, false);
				Int3 indices3 = ParseIndices(split3, ranges3);

				packs1[Interlocked.Increment(ref triangles1Length) - 1] = new IndicesPack
				(
					new Int3(indices0[0], indices3[0], indices2[0]),
					new Int3(indices0[2], indices3[2], indices2[2]),
					new Int3(indices0[1], indices3[1], indices2[1]),
					materialName
				);
			}
		}
	}

	int currentIndex;

	readonly IndicesPack[] packs0; //Triangles are stored in two different arrays to support loading quads
	readonly IndicesPack[] packs1;

	readonly Float3[] vertices;
	readonly Float3[] normals;
	readonly Float2[] texcoords;

	int TriangleCount => packs0.Length + packs1.Length;

	public bool ReadTriangle(out ITriangleStream.Triangle triangle)
	{
		if (currentIndex == TriangleCount)
		{
			triangle = default;
			return false;
		}

		ref readonly IndicesPack pack = ref Unsafe.NullRef<IndicesPack>();
		if (currentIndex < packs0.Length) pack = ref packs0[currentIndex];
		else pack = ref packs1[currentIndex - packs0.Length];

		if (pack.HasNormal)
		{
			triangle = pack.HasTexcoord ?
				new ITriangleStream.Triangle
				(
					vertices[pack.vertexIndices.X], vertices[pack.vertexIndices.Y], vertices[pack.vertexIndices.Z],
					normals[pack.normalIndices.X], normals[pack.normalIndices.Y], normals[pack.normalIndices.Z],
					texcoords[pack.texcoordIndices.X], texcoords[pack.texcoordIndices.Y], texcoords[pack.texcoordIndices.Z]
				) :
				new ITriangleStream.Triangle
				(
					vertices[pack.vertexIndices.X], vertices[pack.vertexIndices.Y], vertices[pack.vertexIndices.Z],
					normals[pack.normalIndices.X], normals[pack.normalIndices.Y], normals[pack.normalIndices.Z]
				);
		}
		else
		{
			triangle = pack.HasTexcoord ?
				new ITriangleStream.Triangle
				(
					vertices[pack.vertexIndices.X], vertices[pack.vertexIndices.Y], vertices[pack.vertexIndices.Z],
					texcoords[pack.texcoordIndices.X], texcoords[pack.texcoordIndices.Y], texcoords[pack.texcoordIndices.Z]
				) :
				new ITriangleStream.Triangle
				(
					vertices[pack.vertexIndices.X], vertices[pack.vertexIndices.Y], vertices[pack.vertexIndices.Z]
				);
		}

		++currentIndex;
		return true;
	}

	public void Dispose() { }

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
		Int3 result = Int3.NegativeOne;

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

	readonly struct Line
	{
		public Line(string source, int height = -1) : this(source, height, Range.All) { }

		public Line(string source, int height, Range range)
		{
			this.height = height;
			this.source = source;

			(offset, length) = range.GetOffsetAndLength(source.Length);
		}

		public readonly int height;
		public readonly int length;

		readonly string source;
		readonly int offset;

		public bool IsEmpty => length == 0;

		public char this[int value] => source[offset + value];

		public Line this[Range value]
		{
			get
			{
				(int Offset, int Length) set = value.GetOffsetAndLength(length);

				int start = offset + set.Offset;
				return new Line(source, height, start..(start + set.Length));
			}
		}

		public Line Trim()
		{
			int start = 0;
			int end = length;

			while (char.IsWhiteSpace(this[start])) start++;
			while (char.IsWhiteSpace(this[end - 1])) end--;

			return this[start..end];
		}

		public Line Trim(char character)
		{
			int start = 0;
			int end = length;

			while (this[start] == character) start++;
			while (this[end - 1] == character) end--;

			return this[start..end];
		}

		public static implicit operator ReadOnlySpan<char>(Line line) => ((ReadOnlySpan<char>)line.source).Slice(line.offset, line.length);
		public static implicit operator string(Line line) => line.source.Substring(line.offset, line.length);
	}

	readonly struct IndicesPack
	{
		public IndicesPack(in Int3 vertexIndices, in Int3 normalIndices, in Int3 texcoordIndices, string materialName)
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

		public bool HasNormal => normalIndices.X >= 0;
		public bool HasTexcoord => texcoordIndices.X >= 0;
	}
}