using System;
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

	/// <summary>
	/// Allows <see cref="EchoSource"/> access to <see cref="TextureGrid.Wrapper"/> using <see cref="string"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if failed to find a matching <see cref="IWrapper"/>.</exception>
	/// <seealso cref="TextureGrid.Wrapper"/>
	[EchoSourceUsable]
	public string WrapperName
	{
		set => Wrapper = ConditionalReturn(value, "clamp", IWrapper.clamp) ??
						 ConditionalReturn(value, "mirror", IWrapper.mirror) ??
						 ConditionalReturn(value, "repeat", IWrapper.repeat) ??
						 throw new ArgumentOutOfRangeException(nameof(value));
	}

	/// <summary>
	/// Allows <see cref="EchoSource"/> access to <see cref="TextureGrid.Filter"/> using <see cref="string"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if failed to find a matching <see cref="IFilter"/>.</exception>
	/// <seealso cref="TextureGrid.Filter"/>
	[EchoSourceUsable]
	public string FilterName
	{
		set => Filter = ConditionalReturn(value, "point", IFilter.point) ??
						ConditionalReturn(value, "bilinear", IFilter.bilinear) ??
						throw new ArgumentOutOfRangeException(nameof(value));
	}

	public override RGBA128 this[Int2 position] => texture[position];

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

	static T ConditionalReturn<T>(string value, string compare, T item) where T : class =>
		value.Equals(compare, StringComparison.InvariantCultureIgnoreCase) ? item : null;
}