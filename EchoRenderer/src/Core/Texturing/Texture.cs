using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// An infinite area of RGBA four channeled pixel colors.
/// </summary>
public abstract class Texture
{
	protected Texture(IWrapper wrapper) => Wrapper = wrapper;

	IWrapper _wrapper;

	/// <summary>
	/// The <see cref="IWrapper"/> used on this <see cref="Texture"/> to control uv texture coordinates.
	/// </summary>
	public IWrapper Wrapper
	{
		get => _wrapper;
		set => _wrapper = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
	}

	/// <summary>
	/// The final <see cref="Tint"/> applied to this <see cref="Texture"/>.
	/// </summary>
	public Tint Tint { get; set; } = Tint.identity;

	/// <summary>
	/// The resolution that should be used if we are performing importance sampling on this <see cref="Texture"/>.
	/// </summary>
	public virtual Int2 ImportanceSamplingResolution => (Int2)512;

	public static readonly Pure white = new(Float3.one);
	public static readonly Pure black = new(Float3.zero);
	public static readonly Pure normal = new(new Float3(0.5f, 0.5f, 1f));

	/// <summary>
	/// Access the content of this <see cref="Texture"/> at <paramref name="uv"/>.
	/// </summary>
	public Vector128<float> this[Float2 uv] => Tint.Apply(Evaluate(Wrapper.Convert(uv)));

	/// <summary>
	/// Gets the pixel data at the indicated texture coordinate <paramref name="uv"/>.
	/// NOTE: the uv is boundless and the specific range is based on <see cref="Wrapper"/>.
	/// </summary>
	protected abstract Vector128<float> Evaluate(Float2 uv);
}