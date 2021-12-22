using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// An infinite area of RGBA four channeled pixel colors.
	/// </summary>
	public abstract class Texture
	{
		protected Texture(IWrapper wrapper) => Wrapper = wrapper;

		IWrapper _wrapper;

		/// <summary>
		/// The <see cref="IWrapper"/> used for this <see cref="Texture"/>.
		/// </summary>
		public IWrapper Wrapper
		{
			get => _wrapper;
			set => _wrapper = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		/// <summary>
		/// The <see cref="Tint"/> applied to this <see cref="Texture"/>.
		/// </summary>
		public Tint Tint { get; set; } = Tint.identity;

		public static readonly Pure white = new(Float4.one);
		public static readonly Pure black = new(Float4.ana);
		public static readonly Pure normal = new(new Float4(0.5f, 0.5f, 1f, 1f));

		/// <summary>
		/// Access the content of this <see cref="Texture"/> at <paramref name="uv"/>.
		/// </summary>
		public Vector128<float> this[Float2 uv]
		{
			get
			{
				var pixel = GetPixel(Wrapper.Convert(uv));
				Tint.Apply(ref pixel);
				return pixel;
			}
		}

		/// <summary>
		/// Gets and returns the pixel data at the indicated texture coordinate <paramref name="uv"/>.
		/// NOTE: the uv is boundless and the specific range is based on <see cref="Wrapper"/>.
		/// </summary>
		protected abstract Vector128<float> GetPixel(Float2 uv);
	}
}