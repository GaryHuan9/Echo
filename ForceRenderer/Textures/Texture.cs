using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	/// <summary>
	/// An asset object used to read or save an image. Pixels are stored raw for fast access but uses much more memory.
	/// Because most textures store data as 32 bit floats, they support the full range of a float.
	/// File operations handled by <see cref="Bitmap"/>. Can be offloaded to separate threads for faster loading.
	/// </summary>
	public abstract class Texture
	{
		protected Texture(Int2 size)
		{
			this.size = size;
			oneLess = size - Int2.one;

			aspect = (float)size.x / size.y;
			length = size.Product;

			_wrapper = Textures.Wrapper.repeat;
			_filter = Textures.Filter.bilinear;
		}

		static Texture()
		{
			white = new Texture2D(Int2.one) {Wrapper = Textures.Wrapper.clamp, Filter = Textures.Filter.point, [Int2.one] = Vector128.Create(1f, 1f, 1f, 1f)};
			black = new Texture2D(Int2.one) {Wrapper = Textures.Wrapper.clamp, Filter = Textures.Filter.point, [Int2.one] = Vector128.Create(0f, 0f, 0f, 1f)};
			normal = new Texture2D(Int2.one) {Wrapper = Textures.Wrapper.clamp, Filter = Textures.Filter.point, [Int2.one] = Vector128.Create(0.5f, 0.5f, 1f, 1f)};
		}

		public readonly Int2 size;
		public readonly Int2 oneLess;

		public readonly float aspect; //Width over height
		protected readonly int length;

		public static readonly Texture white;
		public static readonly Texture black;
		public static readonly Texture normal;

		IWrapper _wrapper;
		IFilter _filter;

		public IWrapper Wrapper
		{
			get => _wrapper;
			set => _wrapper = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		public IFilter Filter
		{
			get => _filter;
			set => _filter = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		/// <summary>
		/// Retrieves and assigns the RGBA color of a pixel based on its index. Index based on <see cref="ToIndex"/> and <see cref="ToPosition"/>.
		/// </summary>
		public abstract Vector128<float> this[int index] { get; set; }

		public virtual Vector128<float> this[Int2 position]
		{
			get => this[ToIndex(Wrapper.Convert(this, position))];
			set => this[ToIndex(Wrapper.Convert(this, position))] = value;
		}

		public virtual Vector128<float> this[Float2 uv] => Filter.Convert(this, Wrapper.Convert(uv));

		public virtual int ToIndex(Int2 position) => position.x + (oneLess.y - position.y) * size.x;
		public virtual Int2 ToPosition(int index) => new Int2(index % size.x, oneLess.y - index / size.x);

		public Int2 Restrict(Int2 position) => position.Clamp(Int2.zero, oneLess);

		public void SetPixel(Int2 position, Float4 pixel) => this[position] = Unsafe.As<Float4, Vector128<float>>(ref pixel);

		public void SetPixel(Int2 position, Float3 pixel)
		{
			Float4 float4 = new Float4(pixel.x, pixel.y, pixel.z, 1f);
			this[position] = Unsafe.As<Float4, Vector128<float>>(ref float4);
		}

		public Float4 GetPixel(Int2 position)
		{
			Vector128<float> pixel = this[position];
			return Unsafe.As<Vector128<float>, Float4>(ref pixel);
		}

		public Float4 GetPixel(Float2 uv)
		{
			Vector128<float> pixel = this[uv];
			return Unsafe.As<Vector128<float>, Float4>(ref pixel);
		}

		/// <summary>
		/// Copies the data of a <see cref="Texture"/> of the same size pixel by pixel.
		/// An exception will be thrown if the sizes mismatch.
		/// </summary>
		public virtual void CopyFrom(Texture texture)
		{
			AssertAlignedSize(texture);

			//Assigns the colors using positions instead of indices because the index converters can be overloaded
			foreach (Int2 position in size.Loop()) this[position] = texture[position];
		}

		protected void AssertAlignedSize(Texture texture)
		{
			if (texture.size == size) return;
			throw ExceptionHelper.Invalid(nameof(texture), texture, "has a mismatched size!");
		}

		public override string ToString() => $"{GetType()} with size {size}";
	}

	public static class Wrapper
	{
		public static readonly IWrapper clamp = new Clamp();
		public static readonly IWrapper repeat = new Repeat();

		class Clamp : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Clamp(0f, 1f);
			public Int2 Convert(Texture texture, Int2 position) => position.Clamp(Int2.zero, texture.oneLess);
		}

		class Repeat : IWrapper
		{
			public Float2 Convert(Float2 uv) => uv.Repeat(1f);
			public Int2 Convert(Texture texture, Int2 position) => position.Repeat(texture.size);
		}
	}

	public static class Filter
	{
		public static readonly IFilter point = new Point();
		public static readonly IFilter bilinear = new Bilinear();

		class Point : IFilter
		{
			public Vector128<float> Convert(Texture texture, Float2 uv)
			{
				Int2 position = (uv * texture.size).Floored;
				return texture[position.Min(texture.oneLess)];
			}
		}

		class Bilinear : IFilter
		{
			public Vector128<float> Convert(Texture texture, Float2 uv)
			{
				uv *= texture.size;

				Int2 upperRight = uv.Rounded;
				Int2 bottomLeft = upperRight - Int2.one;

				//Prefetch color data (273.6 ns => 194.6 ns)
				Vector128<float> y0x0 = texture[bottomLeft];
				Vector128<float> y0x1 = texture[new Int2(upperRight.x, bottomLeft.y)];

				Vector128<float> y1x0 = texture[new Int2(bottomLeft.x, upperRight.y)];
				Vector128<float> y1x1 = texture[upperRight];

				//Interpolate
				Float2 t = Int2.InverseLerp(bottomLeft, upperRight, uv - Float2.half).Clamp(0f, 1f);

				Vector128<float> timeX = Vector128.Create(t.x);
				Vector128<float> timeY = Vector128.Create(t.y);

				Vector128<float> y0 = Lerp(y0x0, y0x1, timeX);
				Vector128<float> y1 = Lerp(y1x0, y1x1, timeX);

				return Lerp(y0, y1, timeY);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
			{
				Vector128<float> length = Sse.Subtract(right, left);

				if (Fma.IsSupported) return Fma.MultiplyAdd(length, time, left);
				return Sse.Add(Sse.Multiply(length, time), left);
			}
		}
	}

	/// <summary>
	/// How to manipulate a uv coordinate if it is out of the zero to one bounds?
	/// </summary>
	public interface IWrapper
	{
		/// <summary>
		/// Converts a uv into a texture coordinate that is between the bounds zero and one.
		/// </summary>
		Float2 Convert(Float2 uv);

		/// <summary>
		/// Converts a position into a texture position that is between the bounds zero and oneLess.
		/// </summary>
		Int2 Convert(Texture texture, Int2 position);
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
		Vector128<float> Convert(Texture texture, Float2 uv);
	}
}