using System;
using CodeHelpers.Packed;
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

	public static readonly Pure white = new(RGBA128.White);
	public static readonly Pure black = new(RGBA128.Black);
	public static readonly Pure normal = new(new RGBA128(0.5f, 0.5f, 1f));

	/// <summary>
	/// Accesses the content of this <see cref="Texture"/> at <paramref name="texcoord"/>.
	/// </summary>
	public RGBA128 this[Float2 texcoord] => Tint.Apply(Evaluate(texcoord));

	/// <summary>
	/// Copies as much data from <paramref name="texture"/> to this <see cref="Texture"/>.
	/// </summary>
	public virtual void CopyFrom(Texture texture) => throw new NotSupportedException();

	/// <summary>
	/// Gets the <see cref="RGBA128"/> pixel data at the indicated texture
	/// coordinate <paramref name="uv"/>. Note that the uv is boundless.
	/// </summary>
	protected abstract RGBA128 Evaluate(Float2 uv);
}