using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeHelpers.Packed;

namespace Echo.Core.InOut;

public class PLYModelLoader
{
	public PLYModelLoader(string path)
	{
		_file = new FileStream(path, FileMode.Open);

		// check for magic number

		string line = ReadLine();
		if (line != "ply") throw new Exception($"Error loading {path}");

		// read the header
		int propertyIndex = 0;
		header.propertiesPositions = new Dictionary<Properties, int>();
		while (true)
		{
			string[] tokens = ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			switch (tokens[0])
			{
				case "comment":
					continue;
				case "format":
					header.format = tokens[1] switch
					{
						"ascii"                => Format.ASCII,
						"binary_little_endian" => Format.BINARY_LITTLE_ENDIAN,
						"binary_big_endian"    => Format.BINARY_BIG_ENDIAN,
						_                      => throw new Exception($"PLY specified format {tokens[1]} is invalid!")
					};
					break;
				case "element":
					if (tokens[1] == "vertex") header.vertexAmount = int.Parse(tokens[2]);
					else if (tokens[1] == "face") header.faceAmount = int.Parse(tokens[2]);
					break;
				case "property":
					if (tokens[1] == "list")
					{
						break;
					}

					header.propertiesPositions.Add(
						tokens[2] switch
						{
							"x"  => Properties.X,
							"y"  => Properties.Y,
							"z"  => Properties.Z,
							"nx" => Properties.NX,
							"ny" => Properties.NY,
							"nz" => Properties.NZ,
							"s"  => Properties.S,
							"t"  => Properties.T
						},
						propertyIndex++
					);
					break;
				case "end_header":
					goto header_finished;
			}
		}

	header_finished: ;
	}

	public Triangle ReadTriangle()
	{
		throw new NotImplementedException();
	}

	string ReadLine()
	{
		buffer.Clear();
		for (byte currentChar = 0x00; Convert.ToChar(currentChar) != '\n';)
		{
			currentChar = (byte)_file.ReadByte();
			buffer.Add(Convert.ToChar(currentChar));
		}

		// remove the \n char at the end as it's not needed
		buffer.RemoveAt(buffer.Count - 1);
		return new(buffer.ToArray());
	}

	readonly List<char> buffer = new();
	readonly FileStream _file;
	Header header;

	public struct Triangle
	{
		public Float3[] vertices;
		public Float3[] normals;
		public Float2[] texcoords;
	}

	struct Header
	{
		public Format format;
		public int vertexAmount;
		public int faceAmount;
		public Dictionary<Properties, int> propertiesPositions;
	}

	enum Format
	{
		ASCII,
		BINARY_LITTLE_ENDIAN,
		BINARY_BIG_ENDIAN
	}

	enum Properties
	{
		// position
		X,
		Y,
		Z,
		// normal
		NX,
		NY,
		NZ,
		// texture coords
		S,
		T,
		NONE
	}

}