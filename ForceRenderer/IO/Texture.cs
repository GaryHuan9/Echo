using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace ForceRenderer.IO
{
	/// <summary>
	/// An asset object used to read or save an image. Pixels are stored raw for fast access but uses much more memory.
	/// File operation handled by <see cref="Bitmap"/>. Can be offloaded to separate threads for faster loading.
	/// </summary>
	public abstract class Texture : LoadableAsset
	{
		protected Texture(Int2 size, bool isReadonly = false) : this(size, isReadonly, IO.Wrapper.clamp, IO.Filter.bilinear) { }

		protected Texture(Int2 size, bool isReadonly, IWrapper wrapper, IFilter filter)
		{
			Wrapper = wrapper; //Has to assign wrappers and filters first because
			Filter = filter;   //the property will throw an exception if is readonly is true

			this.size = size;
			this.isReadonly = isReadonly;

			oneLess = size - Int2.one;
			aspect = (float)size.x / size.y;
			length = size.Product;
		}

		public readonly Int2 size;
		public readonly bool isReadonly;

		public readonly Int2 oneLess;
		public readonly float aspect; //Width over height
		protected readonly int length;

		IWrapper _wrapper;
		IFilter _filter;

		public IWrapper Wrapper
		{
			get => _wrapper;
			set
			{
				if (value == null) throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);

				CheckReadonly();
				_wrapper = value;
			}
		}

		public IFilter Filter
		{
			get => _filter;
			set
			{
				if (value == null) throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);

				CheckReadonly();
				_filter = value;
			}
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif"});
		protected static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new ReadOnlyCollection<ImageFormat>(new[] {ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif});

		protected override IReadOnlyList<string> AcceptableFileExtensions => _acceptableFileExtensions;

		/// <summary>
		/// Retrieves and assigns the RGB color of a pixel based on its index. Index based on <see cref="ToIndex"/> and <see cref="ToPosition"/>.
		/// Should check for readonly on setter using the <see cref="CheckReadonly"/> method to ensure readonly correctness.
		/// </summary>
		public abstract Float3 this[int index] { get; set; }

		public virtual Float3 this[Int2 position]
		{
			get => this[ToIndex(position)];
			set => this[ToIndex(position)] = value;
		}

		public virtual Float3 this[Float2 uv] => Filter.Convert(this, Wrapper.Convert(uv));

		public virtual int ToIndex(Int2 position) => position.x + (oneLess.y - position.y) * size.x;
		public virtual Int2 ToPosition(int index) => new Int2(index % size.x, oneLess.y - index / size.x);

		protected void CheckReadonly()
		{
			if (!isReadonly) return;
			throw new ReadOnlyException();
		}

		/// <summary>
		/// Creates a deep clone of this texture. Should always return a texture of the same type.
		/// <paramref name="newReadonly"/> indicates if the new texture is readonly.
		/// </summary>
		public abstract Texture Clone(bool newReadonly = false);
	}

	public static class Wrapper
	{
		public static readonly IWrapper clamp = new Clamp();
		public static readonly IWrapper repeat = new Repeat();

		class Clamp : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Clamp(0f, 1f);
		}

		class Repeat : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Repeat(1f);
		}
	}

	public static class Filter
	{
		public static readonly IFilter point = new Point();
		public static readonly IFilter bilinear = new Bilinear();

		class Point : IFilter
		{
			public Float3 Convert(Texture texture, Float2 uv) => texture[(uv * texture.size).Floored];
		}

		class Bilinear : IFilter
		{
			public Float3 Convert(Texture texture, Float2 uv)
			{
				uv *= texture.size;
				Int2 rounded = uv.Rounded;

				Int2 upperRight = rounded.Min(texture.oneLess);
				Int2 bottomLeft = rounded.Max(Int2.one) - Int2.one;

				Float2 t = Int2.InverseLerp(bottomLeft, upperRight, uv - Float2.half).Clamp(0f, 1f);

				Float3 y0 = Float3.Lerp(texture[bottomLeft], texture[new Int2(upperRight.x, bottomLeft.y)], t.x);
				Float3 y1 = Float3.Lerp(texture[new Int2(bottomLeft.x, upperRight.y)], texture[upperRight], t.x);

				return Float3.Lerp(y0, y1, t.y);
			}
		}
	}

	/// <summary>
	/// How to manipulate a uv coordinate if it is out of the zero to one bounds?
	/// </summary>
	public interface IWrapper
	{
		/// <summary>
		/// Converts a uv into a texture coordinate that is between the bounds zero to one.
		/// </summary>
		Float2 Convert(Float2 uv);
	}

	/// <summary>
	/// Retrieves the color of a texture using a texture coordinate.
	/// </summary>
	public interface IFilter
	{
		/// <summary>
		/// Returns the color of the texture at <see cref="uv"/>.
		/// </summary>
		/// <param name="texture">The target texture to retrieve the color from.</param>
		/// <param name="uv">The texture coordinate. Must be between zero and one.</param>
		Float3 Convert(Texture texture, Float2 uv);
	}
}