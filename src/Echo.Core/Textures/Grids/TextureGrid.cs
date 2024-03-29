using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grids;

/// <summary>
/// A <see cref="Texture"/> only defined on integer positions and bounded between zero (inclusive) and <see cref="size"/> (exclusive).
/// </summary>
public abstract class TextureGrid : Texture
{
	protected TextureGrid(Int2 size)
	{
		if (!(size > Int2.Zero)) throw new ArgumentOutOfRangeException(nameof(size));

		this.size = size;
		sizeR = 1f / size;
		oneLess = size - Int2.One;

		aspects = new Float2
		(
			(float)size.X / size.Y,
			(float)size.Y / size.X
		);

		power = IsPowerOfTwo(size);

		Wrapper = IWrapper.clamp;
		Filter = IFilter.bilinear;
	}

	/// <summary>
	/// The size of this <see cref="TextureGrid"/> (exclusive),
	/// </summary>
	public readonly Int2 size;

	/// <summary>
	/// The reciprocal of <see cref="size"/>, which is also the normalized size of one pixel.
	/// </summary>
	public readonly Float2 sizeR;

	/// <summary>
	/// The <see cref="size"/> of this <see cref="TextureGrid"/> minus <see cref="Int2.One"/>.
	/// </summary>
	public readonly Int2 oneLess;

	/// <summary>
	/// The aspect ratios of this <see cref="TextureGrid"/>,
	/// the <see cref="Float2.X"/> component equals to width over height, and
	/// the <see cref="Float2.Y"/> component equals to height over width.
	/// </summary>
	public readonly Float2 aspects;

	/// <summary>
	/// If the <see cref="size"/> of this <see cref="TextureGrid"/> is a power of two on any axis, then the
	/// respective component of this field will be that power, otherwise the component will be a negative number.
	/// For example, a <see cref="size"/> of (512, 384) will give (9, -N), where -N is a positive number.
	/// </summary>
	public readonly Int2 power;

	NotNull<IWrapper> _wrapper;
	NotNull<IFilter> _filter;

	/// <summary>
	/// The <see cref="IWrapper"/> used on this <see cref="TextureGrid"/> to convert uv texture coordinates.
	/// </summary>
	public IWrapper Wrapper
	{
		get => _wrapper.Value;
		set => _wrapper = new NotNull<IWrapper>(value);
	}

	/// <summary>
	/// The <see cref="IFilter"/> used on this <see cref="TextureGrid"/> to retrieve pixels as <see cref="RGBA128"/>.
	/// </summary>
	public IFilter Filter
	{
		get => _filter.Value;
		set => _filter = new NotNull<IFilter>(value);
	}

	/// <summary>
	/// Returns the logarithmic average of the two <see cref="size"/> axes.
	/// </summary>
	public float LogSize
	{
		get
		{
			float logWidth = MathF.Log(size.X);
			float logHeight = MathF.Log(size.Y);

			return MathF.Exp((logWidth + logHeight) / 2f);
		}
	}

	public override Int2 DiscreteResolution => size;

	public sealed override RGBA128 this[Float2 texcoord]
	{
		get
		{
			Ensure.IsTrue(float.IsFinite(texcoord.Sum));
			return Filter.Evaluate(this, texcoord);
		}
	}

	/// <summary>
	/// Gets the pixel (<see cref="RGBA128"/>) value of this <see cref="TextureGrid"/> at a <paramref name="position"/>.
	/// </summary>
	/// <param name="position">The integral pixel position to get the value from. This <see cref="Int2"/> must
	/// be between <see cref="Int2.Zero"/> (inclusive) and <see cref="TextureGrid.size"/> (exclusive).</param>
	public abstract RGBA128 this[Int2 position] { get; }

	/// <summary>
	/// Saves this <see cref="TextureGrid"/> to a file.
	/// </summary>
	/// <param name="path">The destination <see cref="string"/> path to use.</param>
	/// <param name="serializer">The <see cref="Serializer"/> to use. An automatic attempt will be made to
	/// find the best <see cref="Serializer"/> from <paramref name="path"/> if this value is null.</param>
	public abstract void Save(string path, Serializer serializer = null);

	/// <summary>
	/// Loads a <see cref="TextureGrid"/> from a file.
	/// </summary>
	/// <param name="path">The source <see cref="string"/> path to load from.</param>
	/// <param name="serializer">The <see cref="Serializer"/> to use. An automatic attempt will be made to
	/// find the best <see cref="Serializer"/> from <paramref name="path"/> if this value is null.</param>
	/// <remarks>This method is identical to <see cref="Load{T}"/>, with the only difference being that this will
	/// create an <see cref="ArrayGrid{T}"/> of the same generic type as this <see cref="TextureGrid"/>.</remarks>
	public abstract TextureGrid Load(string path, Serializer serializer = null);

	/// <summary>
	/// Converts a texture coordinate to an integral pixel position.
	/// </summary>
	/// <param name="uv">The texture coordinate to convert.</param>
	/// <remarks>This operation is not bounded by this <see cref="TextureGrid"/>'s <see cref="size"/>.</remarks>
	public Int2 ToPosition(Float2 uv) => (uv * size).Floored;

	/// <summary>
	/// Converts an integral pixel position to a texture coordinate.
	/// </summary>
	/// <param name="position">The integral pixel position to convert.</param>
	/// <remarks>This operation is not bounded by this <see cref="TextureGrid"/>'s <see cref="size"/>.</remarks>
	public Float2 ToUV(Int2 position) => (position + Float2.Half) * sizeR;

	/// <summary>
	/// Ensures an <see cref="Int2"/> is within the bounds of this <see cref="TextureGrid"/>.
	/// </summary>
	/// <param name="position">The <see cref="Int2"/> to ensure that is within bounds.</param>
	protected void AssertValidPosition(Int2 position) => AssertValidPosition(position, size);

	/// <summary>
	/// Loads a <see cref="SettableGrid{T}"/> from a file.
	/// </summary>
	/// <param name="path">The source <see cref="string"/> path to load from.</param>
	/// <param name="serializer">The <see cref="Serializer"/> to use. An automatic attempt will be made to
	/// find the best <see cref="Serializer"/> from <paramref name="path"/> if this value is null.</param>
	public static SettableGrid<T> Load<T>(string path, Serializer serializer = null) where T : unmanaged, IColor<T>
	{
		serializer ??= Serializer.Find(path);
		if (serializer == null) throw ExceptionHelper.Invalid(nameof(serializer), "is unable to be found");

		using Stream stream = File.OpenRead(path);
		return serializer.Deserialize<T>(stream);
	}

	/// <summary>
	/// Enumerates through all pixels on <see cref="Texture"/> and invoke <paramref name="action"/>.
	/// </summary>
	public void ForEach(Action<Int2> action, bool parallel = true)
	{
		if (parallel)
		{
			Parallel.ForEach(size.Loop(), action);
			return;
		}

		for (int x = 0; x < size.X; x++)
		for (int y = 0; y < size.X; y++)
		{
			action(new Int2(x, y));
		}
	}

	/// <summary>
	/// Asserts an <see cref="Int2"/> is between zero (inclusive) and a certain <paramref name="size"/> (exclusive).
	/// </summary>
	/// <param name="position">The <see cref="Int2"/> to ensure that is within bounds.</param>
	/// <param name="size">The upper limit of the bounds (exclusive).</param>
	protected static void AssertValidPosition(Int2 position, Int2 size)
	{
		Ensure.IsTrue(Int2.Zero <= position);
		Ensure.IsTrue(position < size);
	}

	static Int2 IsPowerOfTwo(Int2 size)
	{
		Ensure.IsTrue(size > Int2.Zero);

		return new Int2
		(
			BitOperations.IsPow2(size.X) ? BitOperations.Log2((uint)size.X) : -1,
			BitOperations.IsPow2(size.Y) ? BitOperations.Log2((uint)size.Y) : -1
		);
	}
}

/// <inheritdoc cref="TextureGrid"/>
/// <typeparam name="T">The type of <see cref="IColor{T}"/> to use.</typeparam>
/// <remarks>This class is the generic variant of <see cref="TextureGrid"/>. Under normal usage, this class should be 
/// preferred over the non-generic version. Only use the non-generic version when a common base class ie needed.</remarks>
public abstract class TextureGrid<T> : TextureGrid where T : unmanaged, IColor<T>
{
	protected TextureGrid(Int2 size) : base(size)
	{
		if (size.MinComponent <= 0) throw new ArgumentOutOfRangeException(nameof(size));
	}

	public sealed override RGBA128 this[Int2 position] => Get(position).ToRGBA128();

	/// <summary>
	/// Gets the pixel value of type <see cref="T"/> of this <see cref="TextureGrid{T}"/> at a <paramref name="position"/>.
	/// </summary>
	/// <param name="position">The integral pixel position to get the value from. This <see cref="Int2"/> must 
	/// be between <see cref="Int2.Zero"/> (inclusive) and <see cref="TextureGrid{T}.size"/> (exclusive).</param>
	/// <remarks>This is identical to <see cref="TextureGrid.Item(Int2)"/>, except the return type.</remarks>
	public abstract T Get(Int2 position);

	public sealed override void Save(string path, Serializer serializer = null)
	{
		serializer ??= Serializer.Find(path);
		if (serializer == null) throw ExceptionHelper.Invalid(nameof(serializer), "cannot be found");

		using Stream stream = File.Open(path, FileMode.Create);
		serializer.Serialize(this, stream);
	}

	public sealed override TextureGrid Load(string path, Serializer serializer = null) => Load<T>(path, serializer);

	/// <summary>
	/// Slices this <see cref="TextureGrid{T}"/> by cropping.
	/// </summary>
	/// <returns>A new <see cref="TextureGrid{T}"/> that is a view of this <see cref="TextureGrid{T}"/>
	/// from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive).</returns>
	public virtual TextureGrid<T> Crop(Int2 min, Int2 max) => new CropGrid(this, min, max);

	class CropGrid : TextureGrid<T>
	{
		public CropGrid(TextureGrid<T> texture, Int2 min, Int2 max) : base(max - min)
		{
			if (!(Int2.Zero <= min) || !(max <= texture.size) || !(min < max)) throw new ArgumentException($"Invalid {nameof(min)} or {nameof(max)}: {min} and {max}.");

			source = texture;
			this.min = min;

			Wrapper = texture.Wrapper;
			Filter = texture.Filter;
		}

		readonly TextureGrid<T> source;
		readonly Int2 min;

		public override T Get(Int2 position) => source.Get(min + position);
	}
}