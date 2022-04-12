global using TextureGrid = EchoRenderer.Core.Texturing.Grid.TextureGrid<EchoRenderer.Common.Coloring.RGB128>;
//
using System;
using System.Numerics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

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
	/// The size of this <see cref="TextureGrid"/> (exclusive),
	/// </summary>
	public readonly Int2 size;

	/// <summary>
	/// The reciprocal of <see cref="size"/>.
	/// </summary>
	public readonly Float2 sizeR;

	/// <summary>
	/// The <see cref="size"/> of this <see cref="TextureGrid"/> minus <see cref="Int2.One"/>.
	/// </summary>
	public readonly Int2 oneLess;

	/// <summary>
	/// The aspect ratio of this <see cref="TextureGrid"/>, equals to width over height.
	/// </summary>
	public readonly float aspect;

	/// <summary>
	/// If the <see cref="size"/> of this <see cref="TextureGrid"/> is a power of two on any axis, then the
	/// respective component of this field will be that power, otherwise the component will be a negative number.
	/// For example, a <see cref="size"/> of (512, 384) will give (9, -N), where N is a positive number.
	/// </summary>
	public readonly Int2 power;

	NotNull<object> _wrapper;
	NotNull<object> _filter;

	/// <summary>
	/// The <see cref="IWrapper"/> used on this <see cref="TextureGrid"/> to convert uv texture coordinates.
	/// </summary>
	public IWrapper Wrapper
	{
		get => (IWrapper)_wrapper.Value;
		set => _wrapper = (object)value;
	}

	/// <summary>
	/// The <see cref="IFilter"/> used on this <see cref="TextureGrid"/> to retrieve pixels as <see cref="RGBA128"/>.
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

	protected sealed override RGBA128 Evaluate(Float2 uv)
	{
		Assert.IsTrue(float.IsFinite(uv.Sum));
		return Filter.Evaluate(this, uv);
	}

	/// <summary>
	/// Converts texture coordinate <paramref name="uv"/> to a integer position based on this <see cref="TextureGrid.size"/>.
	/// </summary>
	public Int2 ToPosition(Float2 uv) => (uv * size).Floored.Clamp(Int2.Zero, oneLess);

	/// <summary>
	/// Converts a pixel integer <paramref name="position"/> to this <see cref="TextureGrid"/>'s texture coordinate.
	/// </summary>
	public Float2 ToUV(Int2 position) => (position + Float2.Half) * sizeR;

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

	protected void AssertAlignedSize(TextureGrid<T> texture)
	{
		if (texture.size == size) return;
		throw ExceptionHelper.Invalid(nameof(texture), texture, "has a mismatched size!");
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