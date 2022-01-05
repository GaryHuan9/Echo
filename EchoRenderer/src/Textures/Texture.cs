using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;

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
		public Vector128<float> this[Float2 uv]
		{
			get
			{
				uv = Wrapper.Convert(uv);
				return Tint.Apply(Evaluate(uv));
			}
		}

		/// <summary>
		/// Gets the pixel data at the indicated texture coordinate <paramref name="uv"/>.
		/// NOTE: the uv is boundless and the specific range is based on <see cref="Wrapper"/>.
		/// </summary>
		protected abstract Vector128<float> Evaluate(Float2 uv);
	}

	/// <summary>
	/// A custom linear transform that can be applied to a color.
	/// </summary>
	public readonly struct Tint
	{
		Tint(in Float4 scale, in Float4 offset)
		{
			this.scale = Utilities.ToVector(scale);
			this.offset = Utilities.ToVector(offset);
		}

		readonly Vector128<float> scale;
		readonly Vector128<float> offset;

		public static readonly Tint identity = new(Float4.one, Float4.zero);

		public Vector128<float> Apply(in Vector128<float> color) => Utilities.Fused(color, scale, offset);

		public static Tint Scale(in Float4 value) => new(value, Float4.zero);
		public static Tint Scale(in Float3 value) => Scale(Utilities.ToColor(value));

		public static Tint Offset(in Float4 value) => new(Float4.one, value);
		public static Tint Offset(in Float3 value) => Offset((Float4)value);

		public static Tint Inverse(in Float4 value) => new(-value, value);
		public static Tint Inverse(in Float3 value) => Inverse(Utilities.ToColor(value));
		public static Tint Inverse()                => Inverse(Float4.one);
	}
}