using System;
using System.IO;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;

namespace Echo.Core.InOut;

public class DataReader : BinaryReader
{
	public DataReader(Stream input) : base(input) { }

	public Color32 ReadColor32()
	{
		byte r = ReadByte();
		byte g = ReadByte();
		byte b = ReadByte();
		byte a = ReadByte();

		return new Color32(r, g, b, a);
	}

	public Color64 ReadColor64()
	{
		ushort r = ReadUInt16();
		ushort g = ReadUInt16();
		ushort b = ReadUInt16();
		ushort a = ReadUInt16();

		return new Color64(r, g, b, a);
	}

	public Direction ReadDirection() => new Direction(ReadByte());

	public Float2 ReadFloat2() => new Float2(ReadSingle(), ReadSingle());
	public Float3 ReadFloat3() => new Float3(ReadSingle(), ReadSingle(), ReadSingle());
	public Float4 ReadFloat4() => new Float4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

	public Float4x4 ReadFloat4x4() => new Float4x4
	(
		ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
		ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
		ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
		ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle()
	);

	public Int2 ReadInt2() => new Int2(ReadInt32(), ReadInt32());
	public Int3 ReadInt3() => new Int3(ReadInt32(), ReadInt32(), ReadInt32());

	public Versor ReadVersor() => new Versor(ReadFloat4());

	public unsafe Guid ReadGuid()
	{
		Guid guid = default;
		byte* pointer = (byte*)&guid;

		for (int i = 0; i < 16; i++) pointer[i] = ReadByte();

		return guid;
	}

	public byte[] ReadByteArray() => ReadByteArray(ReadUInt32Compact());

	public byte[] ReadByteArray(uint length)
	{
		byte[] array = new byte[length];

		uint readLength = (uint)Read(array);
		Ensure.AreEqual(readLength, length);

		return array;
	}

	/// <summary>
	/// Reads in a compact Int32 encoded with <see cref="DataWriter.WriteCompact(int)"/>
	/// </summary>
	public int ReadInt32Compact()
	{
		ulong value = ReadUInt64Compact();
		bool negative = (value & 0b1) == 1;

		value >>= 1;

		return negative ? (int)-(long)value : (int)value;
	}

	/// <summary>
	/// Reads in a compact UInt32 encoded with <see cref="DataWriter.WriteCompact(uint)"/>
	/// </summary>
	public uint ReadUInt32Compact()
	{
		uint value = 0u;

		const byte Mask = 0b0111_1111;

		for (int i = 0;; i += 7)
		{
			byte part = ReadByte();

			value |= (uint)(part & Mask) << i;
			if ((part & ~Mask) == 0) break;
		}

		return value;
	}

	/// <summary>
	/// Reads in a compact UInt64 encoded with <see cref="DataWriter.WriteCompact(ulong)"/>
	/// </summary>
	public ulong ReadUInt64Compact()
	{
		ulong value = 0u;

		const byte Mask = 0b0111_1111;

		for (int i = 0;; i += 7)
		{
			byte part = ReadByte();

			value |= (ulong)(part & Mask) << i;
			if ((part & ~Mask) == 0) break;
		}

		return value;
	}

	public Int2 ReadInt2Compact() => new Int2(ReadInt32Compact(), ReadInt32Compact());
	public Int3 ReadInt3Compact() => new Int3(ReadInt32Compact(), ReadInt32Compact(), ReadInt32Compact());
}