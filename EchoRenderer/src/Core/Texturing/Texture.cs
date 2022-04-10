using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// An infinite area of RGBA four channeled pixel colors.
/// </summary>
public abstract class Texture
{
	protected Texture(IWrapper wrapper) => Wrapper = wrapper;

	NotNull<object> _wrapper;

	/// <summary>
	/// The <see cref="IWrapper"/> used on this <see cref="Texture"/> to control uv texture coordinates.
	/// </summary>
	public IWrapper Wrapper
	{
		get => (IWrapper)_wrapper.Value;
		set => _wrapper = (object)value;
	}

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
	/// Access the content of this <see cref="Texture"/> at <paramref name="uv"/>.
	/// </summary>
	public RGBA128 this[Float2 uv] => Tint.Apply(Evaluate(Wrapper.Convert(uv)));

	/// <summary>
	/// Gets the pixel data at the indicated texture coordinate <paramref name="uv"/>.
	/// NOTE: the uv is boundless and the specific range is based on <see cref="Wrapper"/>.
	/// </summary>
	protected abstract RGBA128 Evaluate(Float2 uv);
}