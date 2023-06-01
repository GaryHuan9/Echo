using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.InOut.Images;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grids;

/// <summary>
/// A <see cref="TextureGrid"/> specifically used by <see cref="InOut.EchoDescription"/> to import textures with parameters.
/// </summary>
/// <remarks>This is mostly a temporary solution; hopefully we will implement a more robust texture parsing system soon.</remarks>
[EchoSourceUsable]
public sealed class ImportGrid : TextureGrid
{
	[EchoSourceUsable]
	public ImportGrid(ImportPath path, bool alpha = true) :
		base(Import(path, alpha, null, out TextureGrid imported)) => texture = imported;

	[EchoSourceUsable]
	public ImportGrid(ImportPath path, bool alpha, bool sRGB) :
		base(Import(path, alpha, sRGB, out TextureGrid imported)) => texture = imported;

	readonly TextureGrid texture;

	public override RGBA128 this[Float2 texcoord] => texture[texcoord];

	public override void Save(string path, Serializer serializer = null) => texture.Save(path, serializer);
	public override TextureGrid Load(string path, Serializer serializer = null) => texture.Load(path, serializer);

	static Int2 Import(string path, bool alpha, bool? sRGB, out TextureGrid imported)
	{
		var serializer = Serializer.Find(path);

		if (sRGB != null && sRGB != serializer.sRGB) serializer = serializer with { sRGB = sRGB.Value };

		imported = alpha ?
			TextureGrid.Load<RGBA128>(path, serializer) :
			TextureGrid.Load<RGB128>(path, serializer);

		return imported.size;
	}
}