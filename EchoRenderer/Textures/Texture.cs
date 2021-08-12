using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A rectangular area of RGBA four channeled pixel colors.
	/// </summary>
	public abstract class Texture
	{
		protected Texture(IWrapper wrapper) => Wrapper = wrapper;

		public static readonly Pure white = new Pure(Float4.one);
		public static readonly Pure black = new Pure(Float4.ana);
		public static readonly Pure normal = new Pure(new Float4(0.5f, 0.5f, 1f, 1f));

		IWrapper _wrapper;

		public IWrapper Wrapper
		{
			get => _wrapper;
			set => _wrapper = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		public Vector128<float> this[Float2 uv] => GetPixel(Wrapper.Convert(uv));

		/// <summary>
		/// Gets and returns the pixel data at the indicated texture coordinate <paramref name="uv"/>.
		/// NOTE: the uv is boundless and the specific range is based on <see cref="Wrapper"/>.
		/// </summary>
		protected abstract Vector128<float> GetPixel(Float2 uv);
	}
}