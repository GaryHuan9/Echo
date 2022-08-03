using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Echo.Core.Common;
using Echo.Core.Common.Packed;
using Echo.Core.Scenic.Geometries;

namespace Echo.Core.InOut.Models;

public sealed class PolygonFileFormatReader : ITriangleStream
{
	/// <summary>
	/// Initializes a new <see cref="PolygonFileFormatReader"/> to stream triangles from the given .ply file
	/// </summary>
	/// <param name="path">path to the .ply file</param>
	/// <exception cref="Exception">When providing a .ply file with custom properties or a format other than binary_little_endian, an exception will be thrown.</exception>
	public PolygonFileFormatReader(string path)
	{
		file = new FileStream(path, FileMode.Open);

		//Check for magic number
		string line = ReadLine(file, ref buffer);
		if (line != "ply") throw new Exception($"Error loading {path}");

		//Read the header
		header = new Header(file, ref buffer);
		file.Position = header.vertexListStart;

		// read the vertex data from the file into the vertexData array
		int vertexDataSize = header.vertexAmount * header.propertyCount * sizeof(float);
		byte[] vertexDataBytes = new byte[vertexDataSize];
		vertexData = new float[header.vertexAmount * header.propertyCount];
		file.Read(vertexDataBytes, 0, vertexDataSize);
		Buffer.BlockCopy(vertexDataBytes, 0, vertexData, 0, vertexDataSize);

		vertex0Data = new float[header.propertyCount];
		vertex1Data = new float[header.propertyCount];
		vertex2Data = new float[header.propertyCount];

		file.Position = header.faceListStart;
	}

	readonly byte[] buffer = new byte[8];
	readonly FileStream file;
	public readonly Header header;

	uint[] currentFaceValues = Array.Empty<uint>();
	int currentTriangle; //Since a face can contain multiple triangles, we also need to keep track of the current triangle inside the face
	int currentFaceTriangleAmount;

	readonly float[] vertexData;

	readonly float[] vertex0Data;
	readonly float[] vertex1Data;
	readonly float[] vertex2Data;

	uint[] readUintBuffer = Array.Empty<uint>();

	public bool ReadTriangle(out ITriangleStream.Triangle triangle) => throw new NotImplementedException();

	/// <summary>
	/// reads the next triangle from the stream.
	/// </summary>
	/// <returns>a new triangle containing 3 vertices, 3 normals and 3 texture coordinates</returns>
	/// <exception cref="Exception">throws an exception when trying to read more triangles than there are in header.<see cref="Header.triangleAmount"/></exception>
	public Triangle ReadTriangle()
	{
		if (currentTriangle >= currentFaceTriangleAmount)
		{
			//We've read all triangles in the current face so read the next face
			int vertexAmount = file.ReadByte();
			if (vertexAmount == -1)
				throw new Exception("The file has ended but you are still trying to read more triangles!");
			if (currentFaceValues.Length < vertexAmount)
				currentFaceValues = new uint[vertexAmount];
			ReadBinaryInts(vertexAmount);
			Array.Copy(readUintBuffer, 0, currentFaceValues, 0, vertexAmount);
			currentFaceTriangleAmount = vertexAmount - 2;
			currentTriangle = 0;
		}

		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle] * header.propertyCount, vertex0Data, 0, header.propertyCount * sizeof(float));
		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle + 1] * header.propertyCount, vertex1Data, 0, header.propertyCount * sizeof(float));
		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle + 2] * header.propertyCount, vertex2Data, 0, header.propertyCount * sizeof(float));

		Float3 vertex0 = new Float3(
			vertex0Data[header.xPos],
			vertex0Data[header.yPos],
			vertex0Data[header.zPos]);
		Float3 vertex1 = new Float3(
			vertex1Data[header.xPos],
			vertex1Data[header.yPos],
			vertex1Data[header.zPos]);
		Float3 vertex2 = new Float3(
			vertex2Data[header.xPos],
			vertex2Data[header.yPos],
			vertex2Data[header.zPos]);

		Float3 normal0 = new Float3(
			vertex0Data[header.nxPos],
			vertex0Data[header.nyPos],
			vertex0Data[header.nzPos]);
		Float3 normal1 = new Float3(
			vertex1Data[header.nxPos],
			vertex1Data[header.nyPos],
			vertex1Data[header.nzPos]);
		Float3 normal2 = new Float3(
			vertex2Data[header.nxPos],
			vertex2Data[header.nyPos],
			vertex2Data[header.nzPos]);

		Float2 texcoord0 = new Float2(
			vertex0Data[header.sPos],
			vertex0Data[header.tPos]);
		Float2 texcoord1 = new Float2(
			vertex1Data[header.sPos],
			vertex1Data[header.tPos]);
		Float2 texcoord2 = new Float2(
			vertex2Data[header.sPos],
			vertex2Data[header.tPos]);

		currentTriangle++;

		return new Triangle(vertex0, vertex1, vertex2, normal0, normal1, normal2, texcoord0, texcoord1, texcoord2);
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

		return Encoding.ASCII.GetString(buffer, 0, stringSize - 1); //stringSize - 1 to remove the \n
	}

	float ReadBinaryFloat()
	{
		file.Read(buffer, 0, 4);

		return BinaryPrimitives.ReadSingleLittleEndian(buffer);
	}

	uint ReadBinaryUInt()
	{
		file.Read(buffer, 0, 4);

		return BitConverter.ToUInt32(buffer, 0);
	}

	float[] ReadBinaryFloats(int amount)
	{
		float[] floats = new float[amount];
		for (int i = 0; i < amount; i++) floats[i] = ReadBinaryFloat();
		return floats;
	}

	void ReadBinaryInts(int amount)
	{
		if (readUintBuffer.Length < amount)
			readUintBuffer = new uint[amount];
		//for (int i = 0; i < amount; i++) readUintBuffer[i] = ReadBinaryUInt();
		file.Read(buffer, 0, amount * sizeof(uint));
		Buffer.BlockCopy(buffer, 0, readUintBuffer, 0, amount * sizeof(uint));
	}

	public readonly struct Triangle
	{
		public readonly Float3 vertex0, vertex1, vertex2;
		public readonly Float3 normal0, normal1, normal2;
		public readonly Float2 texcoord0, texcoord1, texcoord2;

		public Triangle(Float3 vertex0, Float3 vertex1, Float3 vertex2, Float3 normal0, Float3 normal1, Float3 normal2, Float2 texcoord0, Float2 texcoord1, Float2 texcoord2)
		{
			this.vertex0 = vertex0;
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;
			this.normal0 = normal0;
			this.normal1 = normal1;
			this.normal2 = normal2;
			this.texcoord0 = texcoord0;
			this.texcoord1 = texcoord1;
			this.texcoord2 = texcoord2;
		}

		public override string ToString() => $"v0:{vertex0} v1:{vertex1} v2{vertex2} n0{normal0} n1{normal1} n2{normal2} tex0{texcoord0} tex1{texcoord1} tex2{texcoord2}";
	}

	public readonly struct Header
	{
		public Header(FileStream file, ref byte[] buffer)
		{
			int propertyIndex = 0;
			vertexAmount = 0;
			triangleAmount = 0;
			faceAmount = 0;
			while (true)
			{
				string[] tokens = ReadLine(file, ref buffer).Split(' ', StringSplitOptions.RemoveEmptyEntries);
				switch (tokens[0])
				{
					case "comment": continue;
					case "format":
					{
						// format = tokens[1] switch
						// {
						// 	"ascii"                => Format.ASCII,
						// 	"binary_little_endian" => Format.BINARY_LITTLE_ENDIAN,
						// 	"binary_big_endian"    => Format.BINARY_BIG_ENDIAN,
						// 	_                      => throw new Exception($"PLY specified format {tokens[1]} is invalid!")
						// };
						if (tokens[1] != "binary_little_endian") throw new Exception("Only binary_little_endian format is supported right now!");
						break;
					}
					case "element":
					{
						switch (tokens[1])
						{
							case "vertex":
							{
								vertexAmount = int.Parse(tokens[2]);
								break;
							}
							case "face":
							{
								faceAmount = int.Parse(tokens[2]);
								break;
							}
						}

						break;
					}
					case "property":
					{
						if (tokens[1] == "list") break;
						if (tokens[1] != "float") throw new Exception("Only float properties are supported right now");

						switch (tokens[2])
						{
							case "x":
								xPos = propertyIndex++;
								propertyCount++;
								break;
							case "y":
								yPos = propertyIndex++;
								propertyCount++;
								break;
							case "z":
								zPos = propertyIndex++;
								propertyCount++;
								break;
							case "nx":
								nxPos = propertyIndex++;
								propertyCount++;
								break;
							case "ny":
								nyPos = propertyIndex++;
								propertyCount++;
								break;
							case "nz":
								nzPos = propertyIndex++;
								propertyCount++;
								break;
							case "s":
								sPos = propertyIndex++;
								propertyCount++;
								break;
							case "t":
								tPos = propertyIndex++;
								propertyCount++;
								break;
							default:
								throw new Exception($"property {tokens[2]} is not supported yet!");
						}

						break;
					}
					case "end_header":
					{
						vertexListStart = file.Position;
						//asciiLineStarts = new long[vertexAmount];
						faceListStart = vertexListStart + vertexAmount * sizeof(float) * propertyCount;

						file.Position = faceListStart;

						for (int i = 0; i < faceAmount; i++)
						{
							int vertAmountInFace = file.ReadByte();
							triangleAmount += vertAmountInFace - 2;
							file.Position += vertAmountInFace * sizeof(uint);
						}

						file.Position = vertexListStart;

						goto end;
					}
				}
			}

		end:
			{ }
		}

		public readonly int vertexAmount;
		public readonly int triangleAmount;
		public readonly int faceAmount;

		public readonly int propertyCount = 0;
		public readonly int xPos = 0;
		public readonly int yPos = 0;
		public readonly int zPos = 0;
		public readonly int nxPos = 0;
		public readonly int nyPos = 0;
		public readonly int nzPos = 0;
		public readonly int sPos = 0;
		public readonly int tPos = 0;

		public readonly long vertexListStart;
		public readonly long faceListStart;
	}

	// enum Format
	// {
	// 	ASCII,
	// 	BINARY_LITTLE_ENDIAN,
	// 	BINARY_BIG_ENDIAN
	// }

	public enum Properties
	{
		//Position
		X, Y, Z,

		//Normal
		NX, NY, NZ,

		//Texture coordinates
		S, T
	}

	public void Dispose()
	{
		file?.Dispose();
	}
}