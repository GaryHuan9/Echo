using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Texturing.Serialization;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// A <see cref="Texture"/> only defined on integer positions and bounded between zero (inclusive) and <see cref="size"/> (exclusive).
/// </summary>
public abstract partial class TextureGrid<T> : Texture where T : IColor<T>
{
	protected TextureGrid(Int2 size)
	{
		if (!(size > Int2.Zero)) throw ExceptionHelper.Invalid(nameof(size), size, InvalidType.outOfBounds);

		this.size = size;
		sizeR = 1f / size;
		oneLess = size - Int2.One;

		aspect = (float)size.X / size.Y;
		power = IsPowerOfTwo(size);

		Wrapper = Wrappers.clamp;
		Filter = Filters.bilinear;
	}

	/// <summary>
	/// The size of this <see cref="TextureGrid{T}"/> (exclusive),
	/// </summary>
	public readonly Int2 size;

	/// <summary>
	/// The reciprocal of <see cref="size"/>.
	/// </summary>
	public readonly Float2 sizeR;

	/// <summary>
	/// The <see cref="size"/> of this <see cref="TextureGrid{T}"/> minus <see cref="Int2.One"/>.
	/// </summary>
	public readonly Int2 oneLess;

	/// <summary>
	/// The aspect ratio of this <see cref="TextureGrid{T}"/>, equals to width over height.
	/// </summary>
	public readonly float aspect;

	/// <summary>
	/// If the <see cref="size"/> of this <see cref="TextureGrid{T}"/> is a power of two on any axis, then the
	/// respective component of this field will be that power, otherwise the component will be a negative number.
	/// For example, a <see cref="size"/> of (512, 384) will give (9, -N), where N is a positive number.
	/// </summary>
	public readonly Int2 power;

	NotNull<object> _wrapper;
	NotNull<object> _filter;

	/// <summary>
	/// The <see cref="IWrapper"/> used on this <see cref="TextureGrid{T}"/> to convert uv texture coordinates.
	/// </summary>
	public IWrapper Wrapper
	{
		get => (IWrapper)_wrapper.Value;
		set => _wrapper = (object)value;
	}

	/// <summary>
	/// The <see cref="IFilter"/> used on this <see cref="TextureGrid{T}"/> to retrieve pixels as <see cref="RGBA128"/>.
	/// </summary>
	public IFilter Filter
	{
		get => (IFilter)_filter.Value;
		set => _filter = (object)value;
	}

	/// <summary>
	/// Returns the average of the two <see cref="size"/> axes.
	/// We use an logarithmic equation since the average is nicer.
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

	/// <summary>
	/// Access or assign the pixel value of type <see cref="T"/> at a specific integer <paramref name="position"/>. The input
	/// <paramref name="position"/> must be between <see cref="Int2.Zero"/> (inclusive) and <see cref="oneLess"/> (exclusive).
	/// </summary>
	public abstract T this[Int2 position] { get; set; }

	/// <summary>
	/// Enumerates through all pixels on <see cref="Texture"/> and invoke <paramref name="action"/>.
	/// </summary>
	public virtual void ForEach(Action<Int2> action, bool parallel = true)
	{
		if (parallel) Parallel.ForEach(size.Loop(), action);
		else
		{
			foreach (Int2 position in size.Loop()) action(position);
		}
	}

	public override void CopyFrom(Texture texture) => ForEach
	(
		texture is TextureGrid<T> grid && grid.size == size ?
			position => this[position] = grid[position] :
			position => this[position] = texture[ToUV(position)].As<T>()
	);

	public override string ToString() => $"{base.ToString()} with size {size}";

	/// <summary>
	/// Performs a <see cref="Save"/> operation asynchronously.
	/// </summary>
	public Task SaveAsync(string path, ISerializer serializer = null) => Task.Run(() => Save(path, serializer));

	/// <summary>
	/// Saves this <see cref="TextureGrid{T}"/> to <paramref name="path"/> using <paramref name="serializer"/>. An automatic attempt
	/// will be made to find the best <see cref="ISerializer"/> from <paramref name="path"/> if <paramref name="serializer"/> is null.
	/// </summary>
	public void Save(string path, ISerializer serializer = null)
	{
		serializer ??= ISerializer.Find(path);
		if (serializer == null) throw ExceptionHelper.Invalid(nameof(serializer), "is unable to be found");
		serializer.Serialize(this, File.Open(AssetsUtility.GetAssetsPath(path), FileMode.Create));
	}

	/// <summary>
	/// Converts texture coordinate <paramref name="uv"/> to a integer position based on this <see cref="TextureGrid{T}.size"/>.
	/// </summary>
	public Int2 ToPosition(Float2 uv) => (uv * size).Floored.Clamp(Int2.Zero, oneLess);

	/// <summary>
	/// Converts a pixel integer <paramref name="position"/> to this <see cref="TextureGrid{T}"/>'s texture coordinate.
	/// </summary>
	public Float2 ToUV(Int2 position) => (position + Float2.Half) * sizeR;

	protected sealed override RGBA128 Evaluate(Float2 uv)
	{
		Assert.IsTrue(float.IsFinite(uv.Sum));
		return Filter.Evaluate(this, uv);
	}

	/// <summary>
	/// Performs a <see cref="Load"/> operation asynchronously.
	/// </summary>
	public static Task<TextureGrid<T>> LoadAsync(string path, ISerializer serializer = null) => Task.Run(() => Load(path, serializer));

	/// <summary>
	/// Loads a <see cref="TextureGrid{T}"/> from <paramref name="path"/> using <paramref name="serializer"/>. An automatic attempt
	/// will be made to find the best <see cref="ISerializer"/> from <paramref name="path"/> if <paramref name="serializer"/> is null.
	/// </summary>
	public static TextureGrid<T> Load(string path, ISerializer serializer = null)
	{
		serializer ??= ISerializer.Find(path);
		if (serializer == null) throw ExceptionHelper.Invalid(nameof(serializer), "is unable to be found");
		return serializer.Deserialize<T>(File.OpenRead(AssetsUtility.GetAssetsPath(path)));
	}

	static Int2 IsPowerOfTwo(Int2 size)
	{
		Assert.IsTrue(size > Int2.Zero);

		return new Int2
		(
			size.X.IsPowerOfTwo() ? BitOperations.Log2((uint)size.X) : -1,
			size.Y.IsPowerOfTwo() ? BitOperations.Log2((uint)size.Y) : -1
		);
	}
}