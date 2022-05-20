using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Files;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures.Serialization;

/// <summary>
/// An <see cref="Serializer"/> for the <see cref="Echo"/> custom floating-point image (.fpi) format.
/// </summary>
public record FpiSerializer : Serializer
{
	public static readonly FpiSerializer fpi = new();

	public override void Serialize<T>(TextureGrid<T> texture, Stream stream)
	{
		using var writer = new DataWriter(stream);

		writer.Write(1); //Writes version number
		Write(texture, writer);
	}

	public override TextureGrid<T> Deserialize<T>(Stream stream)
	{
		using var reader = new DataReader(stream);

		int version = reader.ReadInt32(); //Reads version number
		if (version == 0) throw new NotSupportedException();

		return Read<T>(reader);
	}

	static void Write<T>(TextureGrid<T> texture, DataWriter writer) where T : unmanaged, IColor<T>
	{
		writer.WriteCompact(texture.size);
		var sequence = Vector128<uint>.Zero;

		foreach (Int2 position in texture.size.Loop())
		{
			Vector128<uint> current = Cast(texture[position]);
			Vector128<uint> xor = Sse2.Xor(sequence, current);

			//Write the xor difference as variable length quantity for lossless compression

			sequence = current;

			writer.WriteCompact(xor.GetElement(0));
			writer.WriteCompact(xor.GetElement(1));
			writer.WriteCompact(xor.GetElement(2));
			writer.WriteCompact(xor.GetElement(3));
		}

		static unsafe Vector128<uint> Cast(in T value)
		{
			RGBA128 color = value.ToRGBA128();
			return *(Vector128<uint>*)&color;
		}
	}

	static unsafe ArrayGrid<T> Read<T>(DataReader reader) where T : unmanaged, IColor<T>
	{
		Int2 size = reader.ReadInt2Compact();
		var texture = new ArrayGrid<T>(size);

		var sequence = Vector128<uint>.Zero;

		//Read the xor difference sequence

		foreach (Int2 position in size.Loop())
		{
			Vector128<uint> xor = Vector128.Create
			(
				reader.ReadUInt32Compact(),
				reader.ReadUInt32Compact(),
				reader.ReadUInt32Compact(),
				reader.ReadUInt32Compact()
			);

			Vector128<uint> current = Sse2.Xor(sequence, xor);
			texture[position] = (*(RGBA128*)&current).As<T>();

			sequence = current;
		}

		return texture;
	}
}