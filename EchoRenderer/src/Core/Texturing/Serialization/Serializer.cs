using System;
using System.IO;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.Texturing.Serialization;

/// <summary>
/// A serialization interface for saving and loading <see cref="TextureGrid{T}"/> into and out of <see cref="Stream"/>.
/// </summary>
public abstract record Serializer
{
	/// <summary>
	/// Whether the <see cref="Serializer"/> should use the standard RGB color space and gamma
	/// correct the converted data? This property might not be applicable to all implementations.
	/// </summary>
	public bool sRGB { get; init; } = true;

	/// <summary>
	/// Converts and writes <paramref name="texture"/> to <paramref name="stream"/>.
	/// </summary>
	public abstract void Serialize<T>(TextureGrid<T> texture, Stream stream) where T : IColor<T>;

	/// <summary>
	/// Reads and converts <paramref name="stream"/> into a <see cref="TextureGrid{T}"/>.
	/// </summary>
	public abstract TextureGrid<T> Deserialize<T>(Stream stream) where T : IColor<T>;

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

		return new string(span) switch
		{
			"png" => SystemSerializer.png,
			"jpeg" => SystemSerializer.jpeg,
			"jpg" => SystemSerializer.jpeg,
			"tiff" => SystemSerializer.tiff,
			"bmp" => SystemSerializer.bmp,
			"gif" => SystemSerializer.gif,
			"exif" => SystemSerializer.exif,
			"fpi" => FpiSerializer.fpi,
			_ => null
		};
	}
}