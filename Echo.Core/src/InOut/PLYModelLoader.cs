using System;
using System.Collections.Generic;
using System.Drawing;
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
		header = new Header(ref file, ref buffer, ref asciiVertexLineStarts);
		file.Position = header.faceListStart;
	}

	public Triangle ReadTriangle()
	{
		Triangle resultTriangle = new Triangle();

		if (currentTriangle >= currentFaceTriangleAmount)
		{
			// we've read all triangles in the current face so read the next face
			if (header.format == Format.ASCII)
			{
				currentFaceStrings = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				currentFaceValues = new int[currentFaceStrings.Length];
				currentFaceValues = Array.ConvertAll<string, int>(currentFaceStrings, int.Parse);

				currentFaceTriangleAmount = currentFaceValues[0] - 2;
				currentTriangle = 0;
			}
			else
			{
				int vertexAmount = BitConverter.ToChar(new[] { (byte)file.ReadByte() }, 0);
				currentFaceValues = new int[vertexAmount + 1];
				Array.Copy(ReadBinaryInts(file, ref buffer, vertexAmount), 0, currentFaceValues, 1, vertexAmount);
				currentFaceTriangleAmount = vertexAmount - 2;
				currentTriangle = 0;
			}
		}

		long prevPos = file.Position;

		if (header.format == Format.ASCII)
		{
			file.Position = asciiVertexLineStarts[currentFaceValues[currentTriangle + 1]];
			string[] vertex0Data = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			file.Position = asciiVertexLineStarts[currentFaceValues[currentTriangle + 2]];
			string[] vertex1Data = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			file.Position = asciiVertexLineStarts[currentFaceValues[currentTriangle + 3]];
			string[] vertex2Data = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			file.Position = prevPos;

			Console.WriteLine($"vertex0: {currentFaceValues[currentTriangle + 1]} vertex1: {currentFaceValues[currentTriangle + 2]} vertex2: {currentFaceValues[currentTriangle + 3]}");

			resultTriangle.vertex0 = new Float3(
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.X]]),
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.Y]]),
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.Z]]));
			resultTriangle.vertex1 = new Float3(
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.X]]),
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.Y]]),
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.Z]]));
			resultTriangle.vertex2 = new Float3(
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.X]]),
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.Y]]),
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.Z]]));

			resultTriangle.normal0 = new Float3(
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.NX]]),
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.NY]]),
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.NZ]]));
			resultTriangle.normal1 = new Float3(
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.NX]]),
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.NY]]),
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.NZ]]));
			resultTriangle.normal2 = new Float3(
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.NX]]),
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.NY]]),
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.NZ]]));

			resultTriangle.texcoord0 = new Float2(
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.S]]),
				Convert.ToSingle(vertex0Data[header.propertiesPositions[Properties.T]]));
			resultTriangle.texcoord1 = new Float2(
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.S]]),
				Convert.ToSingle(vertex1Data[header.propertiesPositions[Properties.T]]));
			resultTriangle.texcoord2 = new Float2(
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.S]]),
				Convert.ToSingle(vertex2Data[header.propertiesPositions[Properties.T]]));
		}
		else
		{
			file.Position = currentFaceValues[currentTriangle + 1];
			float[] vertex0Data = ReadBinaryFloats(file, ref buffer, header.propertiesPositions.Count);
			file.Position = currentFaceValues[currentTriangle + 2];
			float[] vertex1Data = ReadBinaryFloats(file, ref buffer, header.propertiesPositions.Count);
			file.Position = currentFaceValues[currentTriangle + 3];
			float[] vertex2Data = ReadBinaryFloats(file, ref buffer, header.propertiesPositions.Count);

			resultTriangle.vertex0 = new Float3(
				vertex0Data[header.propertiesPositions[Properties.X]],
				vertex0Data[header.propertiesPositions[Properties.Y]],
				vertex0Data[header.propertiesPositions[Properties.Z]]);
			resultTriangle.vertex1 = new Float3(
				vertex1Data[header.propertiesPositions[Properties.X]],
				vertex1Data[header.propertiesPositions[Properties.Y]],
				vertex1Data[header.propertiesPositions[Properties.Z]]);
			resultTriangle.vertex2 = new Float3(
				vertex2Data[header.propertiesPositions[Properties.X]],
				vertex2Data[header.propertiesPositions[Properties.Y]],
				vertex2Data[header.propertiesPositions[Properties.Z]]);

			resultTriangle.normal0 = new Float3(
				vertex0Data[header.propertiesPositions[Properties.NX]],
				vertex0Data[header.propertiesPositions[Properties.NY]],
				vertex0Data[header.propertiesPositions[Properties.NZ]]);
			resultTriangle.normal1 = new Float3(
				vertex1Data[header.propertiesPositions[Properties.NX]],
				vertex1Data[header.propertiesPositions[Properties.NY]],
				vertex1Data[header.propertiesPositions[Properties.NZ]]);
			resultTriangle.normal2 = new Float3(
				vertex2Data[header.propertiesPositions[Properties.NX]],
				vertex2Data[header.propertiesPositions[Properties.NY]],
				vertex2Data[header.propertiesPositions[Properties.NZ]]);

			resultTriangle.texcoord0 = new Float2(
				vertex0Data[header.propertiesPositions[Properties.S]],
				vertex0Data[header.propertiesPositions[Properties.T]]);
			resultTriangle.texcoord1 = new Float2(
				vertex1Data[header.propertiesPositions[Properties.S]],
				vertex1Data[header.propertiesPositions[Properties.T]]);
			resultTriangle.texcoord2 = new Float2(
				vertex2Data[header.propertiesPositions[Properties.S]],
				vertex2Data[header.propertiesPositions[Properties.T]]);
		}

		currentTriangle++;

		return resultTriangle;
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

	static float ReadBinaryFloat(FileStream file, ref byte[] buffer)
	{
		Utility.EnsureCapacity(ref buffer, 4, true, 4);
		file.Read(buffer, 0, 4);
		return BitConverter.ToSingle(buffer, 0);
	}

	static int ReadBinaryInt(FileStream file, ref byte[] buffer)
	{
		Utility.EnsureCapacity(ref buffer, 4, true, 4);
		file.Read(buffer, 0, 4);
		return BitConverter.ToInt32(buffer, 0);
	}

	static float[] ReadBinaryFloats(FileStream file, ref byte[] buffer, int amount)
	{
		float[] floats = new float[amount];
		for (int i = 0; i < amount; i++) floats[i] = ReadBinaryFloat(file, ref buffer);
		return floats;
	}

	static int[] ReadBinaryInts(FileStream file, ref byte[] buffer, int amount)
	{
		int[] ints = new int[amount];
		for (int i = 0; i < amount; i++) ints[i] = ReadBinaryInt(file, ref buffer);
		return ints;
	}

	byte[] buffer = new byte[1];
	readonly FileStream file;
	Header header;

	int[] currentFaceValues = { };
	int currentTriangle = 0; // since a face can contain multiple triangles, we also need to keep track of the current triangle inside the face
	int currentFaceTriangleAmount;
	string[] currentFaceStrings = { };
	long[] asciiVertexLineStarts = { };


	public struct Triangle
	{
		public Float3 vertex0, vertex1, vertex2;
		public Float3 normal0, normal1, normal2;
		public Float2 texcoord0, texcoord1, texcoord2;

		public override string ToString() => $"v0:{vertex0} v1:{vertex1} v2{vertex2} n0{normal0} n1{normal1} n2{normal2} tex0{texcoord0} tex1{texcoord1} tex2{texcoord2}";
	}

	struct RawVertex
	{
		public float x, y, z, nx, ny, nz, s, t;
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

		public Header(ref FileStream file, ref byte[] buffer, ref long[] asciiLineStarts)
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
						asciiLineStarts = new long[vertexAmount];
						if (format == Format.ASCII)
						{
							int readBytes = 0;
							asciiLineStarts[0] = file.Position;
							for (int readVertices = 0; readVertices < vertexAmount;)
							{
								char readChar = Convert.ToChar(file.ReadByte());
								readBytes++;
								if (readChar == '\n')
								{
									readVertices++;
									if (readVertices < vertexAmount) asciiLineStarts[readVertices] = file.Position;
								}
							}

							faceListStart = vertexListStart + readBytes;
						}
						else
						{
							faceListStart = vertexListStart + faceAmount * sizeof(float) * propertiesPositions.Count;
						}

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