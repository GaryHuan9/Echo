using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut.Images;

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

	public override ArrayGrid<T> Deserialize<T>(Stream stream)
	{
		using var reader = new DataReader(stream);

		int version = reader.ReadInt32(); //Reads version number
		if (version == 0) throw new NotSupportedException();

		return Read<T>(reader);
	}

	static void Write<T>(TextureGrid<T> texture, DataWriter writer) where T : unmanaged, IColor<T>
	{
		writer.WriteCompact(texture.size);
		var previous = Vector128<float>.Zero;

		//Write the xor difference as variable length quantity for lossless compression
		foreach (Int2 position in texture.size.Loop())
		{
			Vector128<float> current = texture[position].ToFloat4().v;
			Vector128<uint> xor = Sse.Xor(previous, current).AsUInt32();

			previous = current;

			writer.WriteCompact(xor.GetElement(0));
			writer.WriteCompact(xor.GetElement(1));
			writer.WriteCompact(xor.GetElement(2));
			writer.WriteCompact(xor.GetElement(3));
		}
	}

	static ArrayGrid<T> Read<T>(DataReader reader) where T : unmanaged, IColor<T>
	{
		Int2 size = reader.ReadInt2Compact();
		var texture = new ArrayGrid<T>(size);
		var previous = Vector128<float>.Zero;

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

			Vector128<float> current = Sse.Xor(previous, xor.AsSingle());
			texture.Set(position, default(T).FromFloat4(new Float4(current)));

			previous = current;
		}

		return texture;
	}
}