using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures;

/// <summary>
/// An infinite two-dimensional plane of data that can be arbitrarily sampled as <see cref="RGBA128"/> four-channelled colors.
/// </summary>
public abstract class Texture
{
	/// <summary>
	/// The resolution that should be used if discrete sampling is to be performed on this <see cref="Texture"/>.
	/// </summary>
	public virtual Int2 DiscreteResolution => (Int2)512;

	/// <summary>
	/// Gets the <see cref="RGBA128"/> color data of this <see cref="Texture"/> at the
	/// indicated texture coordinate <paramref name="texcoord"/>.
	/// </summary>
	/// <remarks>The <paramref name="texcoord"/> is boundless.</remarks>
	public abstract RGBA128 this[Float2 texcoord] { get; }
}