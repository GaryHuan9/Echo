using System;
using System.Data;
using System.Drawing;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	/// <summary>
	/// An asset object used to read or save an image. Pixels are stored raw for fast access but uses much more memory.
	/// File operation handled by <see cref="Bitmap"/>. Can be offloaded to separate threads for faster loading.
	/// </summary>
	public abstract class Texture
	{
		protected Texture(Int2 size, IWrapper wrapper = null, IFilter filter = null)
		{
			this.size = size;
			oneLess = size - Int2.one;

			aspect = (float)size.x / size.y;
			length = size.Product;

			_wrapper = wrapper ?? Textures.Wrapper.clamp;
			_filter = filter ?? Textures.Filter.bilinear;
		}

		public readonly Int2 size;
		public readonly Int2 oneLess;

		public readonly float aspect; //Width over height
		protected readonly int length;

		public bool IsReadonly { get; protected set; }

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

		/// <summary>
		/// Sets this <see cref="Texture"/> as readonly.
		/// NOTE: This operation cannot be reverted.
		/// </summary>
		public void SetReadonly() => IsReadonly = true;

		public virtual int ToIndex(Int2 position) => position.x + (oneLess.y - position.y) * size.x;
		public virtual Int2 ToPosition(int index) => new Int2(index % size.x, oneLess.y - index / size.x);

		protected void CheckReadonly()
		{
			if (IsReadonly) throw new Exception($"Operation cannot be completed when {this} is readonly.");
		}

		/// <summary>
		/// Copies the data of a <see cref="Texture"/> of the same size pixel by pixel.
		/// An exception will be thrown if the sizes mismatch.
		/// </summary>
		public void CopyFrom(Texture texture)
		{
			if (texture.size != size) throw ExceptionHelper.Invalid(nameof(texture), texture, "has a mismatched size!");

			//Assigns the colors using positions instead of indices because the index converters can be overloaded
			foreach (Int2 position in size.Loop()) this[position] = texture[position];
		}

		public override string ToString() => $"{(IsReadonly ? "Readonly" : "Read-write")} {GetType()} with size {size}";
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