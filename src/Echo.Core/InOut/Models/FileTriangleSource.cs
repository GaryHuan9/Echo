using System;
using System.Collections.Generic;
using System.IO;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Geometries;

namespace Echo.Core.InOut.Models;

/// <summary>
/// An implementation of <see cref="ITriangleSource"/> that allows the usage of different 3D model file formats.
/// </summary>
/// <remarks>Currently .ply and .obj files are supported.</remarks>
[EchoSourceUsable]
public sealed class FileTriangleSource : ITriangleSource
{
	[EchoSourceUsable]
	public FileTriangleSource(ImportPath path)
	{
		if (!File.Exists(path)) throw new FileNotFoundException("Triangle source file not found.", path);

		string extension = Path.GetExtension(path);

		if (factories.TryGetValue(extension, out factory)) this.path = path;
		else throw new ArgumentException($"Unrecognized triangle file extension '{extension}'.", nameof(path));
	}

	readonly string path;
	readonly Factory factory;

	static readonly Dictionary<string, Factory> factories = new()
	{
		{ ".ply", path => new PolygonFileFormatReader(path) },
		{ ".obj", path => new WavefrontObjectFormatReader(path) },
		{ ".zip", path => new WavefrontObjectFormatReader(path) } //Legacy
	};

	/// <inheritdoc/>
	public ITriangleStream CreateStream() => factory(path);

	delegate ITriangleStream Factory(string path);
}