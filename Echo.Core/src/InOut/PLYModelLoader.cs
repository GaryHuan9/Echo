using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeHelpers.Packed;
using Echo.Core.Common;

namespace Echo.Core.InOut;

public class PLYModelLoader
{
	public PLYModelLoader(string path)
	{
		file = new FileStream(path, FileMode.Open);

		// check for magic number

		string line = ReadLine(file, ref buffer);
		if (line != "ply") throw new Exception($"Error loading {path}");

		// read the header
		header = new Header(ref file, ref buffer);
		file.Position = header.faceListStart;
		Console.WriteLine(ReadLine(file, ref buffer));
	}

	public Triangle ReadTriangle()
	{
		throw new NotImplementedException();
	}

	static string ReadLine(FileStream file, ref byte[] buffer)
	{
		int stringSize = 0;
		int i = 0;
		while (true)
		{
			Utility.EnsureCapacity(ref buffer, i + 1, true, buffer.Length * 2);
			buffer[i] = (byte)file.ReadByte();
			stringSize++;
			if (Convert.ToChar(buffer[i++]) == '\n') break;
		}

		string result = Encoding.ASCII.GetString(buffer, 0, stringSize - 1); // stringSize - 1 to remove the \n
		return result;
	}

	readonly byte[] buffer = new byte[1];
	readonly FileStream file;
	Header header;

	public struct Triangle
	{
		public Float3 vertex0, vertex1, vertex2;
		public Float3 normal0, normal1, normal2;
		public Float2 texcoord0, texcoord1;
	}

	struct Header
	{
		public Format format;
		public int vertexAmount;
		public int faceAmount;
		public Dictionary<Properties, int> propertiesPositions;

		public long vertexListStart;
		public long faceListStart;

		int vertexSize = 0;

		public Header(ref FileStream file, ref byte[] buffer)
		{
			int propertyIndex = 0;
			propertiesPositions = new Dictionary<Properties, int>();
			format = Format.ASCII;
			vertexAmount = 0;
			faceAmount = 0;
			while (true)
			{
				string[] tokens = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.RemoveEmptyEntries);
				switch (tokens[0])
				{
					case "comment":
						continue;
					case "format":
						format = tokens[1] switch
						{
							"ascii"                => Format.ASCII,
							"binary_little_endian" => Format.BINARY_LITTLE_ENDIAN,
							"binary_big_endian"    => Format.BINARY_BIG_ENDIAN,
							_                      => throw new Exception($"PLY specified format {tokens[1]} is invalid!")
						};
						break;
					case "element":
						switch (tokens[1])
						{
							case "vertex":
								vertexAmount = int.Parse(tokens[2]);
								break;
							case "face":
								faceAmount = int.Parse(tokens[2]);
								break;
						}

						break;
					case "property":
						if (tokens[1] == "list")
						{
							break;
						}

						switch (tokens[2])
						{
							case "x":
								propertiesPositions.Add(Properties.X, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "y":
								propertiesPositions.Add(Properties.Y, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "z":
								propertiesPositions.Add(Properties.Z, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "nx":
								propertiesPositions.Add(Properties.NX, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "ny":
								propertiesPositions.Add(Properties.NY, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "nz":
								propertiesPositions.Add(Properties.NZ, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "s":
								propertiesPositions.Add(Properties.S, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							case "t":
								propertiesPositions.Add(Properties.T, propertyIndex++);
								vertexSize += sizeof(float);
								break;
							default:
								switch (tokens[1])
								{
									case "char":
										vertexSize += 1;
										break;
									case "uchar":
										vertexSize += 1;
										break;
									case "short":
										vertexSize += 2;
										break;
									case "ushort":
										vertexSize += 2;
										break;
									case "int":
										vertexSize += 4;
										break;
									case "uint":
										vertexSize += 4;
										break;
									case "float":
										vertexSize += 4;
										break;
									case "double":
										vertexSize += 8;
										break;
								}

								propertiesPositions.Add(Properties.NONE, propertyIndex++);
								break;
						}

						break;
					case "end_header":
						vertexListStart = file.Position;
						faceListStart = vertexListStart + vertexAmount * vertexSize;
						goto header_finished;
				}
			}

		header_finished: ;
		}
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