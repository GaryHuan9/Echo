using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Echo.Core.Common;
using Echo.Core.Common.Packed;

namespace Echo.Core.InOut;

public class PolygonFileFormatReader
{
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
		int vertexDataSize = header.vertexAmount * header.propertiesPositions.Count * sizeof(float);
		byte[] vertexDataBytes = new byte[vertexDataSize];
		vertexData = new float[header.vertexAmount * header.propertiesPositions.Count];
		file.Read(vertexDataBytes, 0, vertexDataSize);
		Buffer.BlockCopy(vertexDataBytes, 0, vertexData, 0, vertexDataSize);

		vertex0Data = new float[header.propertiesPositions.Count];
		vertex1Data = new float[header.propertiesPositions.Count];
		vertex2Data = new float[header.propertiesPositions.Count];

		file.Position = header.faceListStart;
	}

	readonly byte[] buffer = new byte[8];
	readonly FileStream file;
	public readonly Header header;

	uint[] currentFaceValues = { };
	int currentTriangle; //Since a face can contain multiple triangles, we also need to keep track of the current triangle inside the face
	int currentFaceTriangleAmount;

	readonly float[] vertexData;

	readonly float[] vertex0Data;
	readonly float[] vertex1Data;
	readonly float[] vertex2Data;

	uint[] readUintBuffer = { };

	public Triangle ReadTriangle()
	{
		Triangle resultTriangle = new Triangle();

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

		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle] * header.propertiesPositions.Count, vertex0Data, 0, header.propertiesPositions.Count * sizeof(float));
		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle + 1] * header.propertiesPositions.Count, vertex1Data, 0, header.propertiesPositions.Count * sizeof(float));
		Buffer.BlockCopy(vertexData, (int)currentFaceValues[currentTriangle + 2] * header.propertiesPositions.Count, vertex2Data, 0, header.propertiesPositions.Count * sizeof(float));

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

	public struct Triangle
	{
		public Float3 vertex0, vertex1, vertex2;
		public Float3 normal0, normal1, normal2;
		public Float2 texcoord0, texcoord1, texcoord2;

		public override string ToString() => $"v0:{vertex0} v1:{vertex1} v2{vertex2} n0{normal0} n1{normal1} n2{normal2} tex0{texcoord0} tex1{texcoord1} tex2{texcoord2}";
	}

	public readonly struct Header
	{
		public Header(FileStream file, ref byte[] buffer)
		{
			int propertyIndex = 0;
			propertiesPositions = new Dictionary<Properties, int>();
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

						propertiesPositions.Add(tokens[2] switch
						{
							"x"  => Properties.X,
							"y"  => Properties.Y,
							"z"  => Properties.Z,
							"nx" => Properties.NX,
							"ny" => Properties.NY,
							"nz" => Properties.NZ,
							"s"  => Properties.S,
							"t"  => Properties.T,
							_    => throw new Exception($"property {tokens[2]} is not supported yet!")
						}, propertyIndex++);

						break;
					}
					case "end_header":
					{
						vertexListStart = file.Position;
						//asciiLineStarts = new long[vertexAmount];
						faceListStart = vertexListStart + vertexAmount * sizeof(float) * propertiesPositions.Count;

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
		public readonly Dictionary<Properties, int> propertiesPositions;

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
}