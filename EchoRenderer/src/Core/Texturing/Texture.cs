using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// An infinite area of RGBA four channeled pixel colors.
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
	/// Accesses the content of this <see cref="Texture"/> at <paramref name="uv"/>.
	/// </summary>
	public RGBA128 this[Float2 uv] => Tint.Apply(Evaluate(uv));

	/// <summary>
	/// Gets the <see cref="RGBA128"/> pixel data at the indicated texture
	/// coordinate <paramref name="uv"/>. Note that the uv is boundless.
	/// </summary>
	protected abstract RGBA128 Evaluate(Float2 uv);
}