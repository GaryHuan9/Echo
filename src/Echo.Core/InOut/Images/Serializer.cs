using System;
using System.IO;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut.Images;

/// <summary>
/// A serialization interface for saving and loading <see cref="TextureGrid{T}"/> into and out of <see cref="Stream"/>.
/// </summary>
public abstract record Serializer
{
	/// <summary>
	/// Whether the <see cref="Serializer"/> should use the standard RGB color-space and gamma
	/// correct the converted data? This property might not be applicable to all implementations.
	/// </summary>
	public bool sRGB { get; init; } = true;

	/// <summary>
	/// Converts and writes <paramref name="texture"/> to <paramref name="stream"/>.
	/// </summary>
	public abstract void Serialize<T>(TextureGrid<T> texture, Stream stream) where T : unmanaged, IColor<T>;

	/// <summary>
	/// Reads and converts <paramref name="stream"/> into a <see cref="TextureGrid{T}"/>.
	/// </summary>
	public abstract SettableGrid<T> Deserialize<T>(Stream stream) where T : unmanaged, IColor<T>;

	/// <summary>
	/// Tries to find and return the appropriate <see cref="Serializer"/> for a
	/// file at <paramref name="path"/>. If none can be found, null is returned.
	/// </summary>
	public static Serializer Find(ReadOnlySpan<char> path)
	{
		var extension = Path.GetExtension(path);
		if (extension.Length == 0) return null;

		Span<char> span = stackalloc char[extension.Length];
		span = span[..extension[1..].ToLowerInvariant(span)];

		return new string(span) switch //OPTIMIZE: switch on Span with dotnet 7
		{
			"png" => MagickSerializer.png,
			"jpeg" => MagickSerializer.jpeg,
			"jpg" => MagickSerializer.jpeg,
			"tiff" => MagickSerializer.tiff,
			"exr" => MagickSerializer.exr,
			"hdr" => MagickSerializer.hdr,
			"fpi" => FpiSerializer.fpi,
			_ => null
		};
	}
}