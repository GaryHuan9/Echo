using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures;

/// <summary>
/// An infinite two-dimensional plane of data that can be arbitrarily sampled as <see cref="RGBA128"/> four-channelled colors.
/// </summary>
public abstract class Texture
{
	/// <summary>
	/// The final <see cref="Tint"/> applied to this <see cref="Texture"/>.
	/// </summary>
	public Tint Tint { get; set; } = Tint.Identity;

	/// <summary>
	/// The resolution that should be used if discrete sampling is to be performed on this <see cref="Texture"/>.
	/// </summary>
	public virtual Int2 DiscreteResolution => (Int2)512;

	/// <summary>
	/// Accesses the content of this <see cref="Texture"/> at <paramref name="texcoord"/>.
	/// </summary>
	public RGBA128 this[Float2 texcoord] => Tint.Apply(Evaluate(texcoord));

	/// <summary>
	/// Gets the <see cref="RGBA128"/> pixel data at the indicated texture
	/// coordinate <paramref name="uv"/>. Note that the uv is boundless.
	/// </summary>
	protected abstract RGBA128 Evaluate(Float2 uv);
}