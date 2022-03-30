using System;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// A <see cref="Texture"/> only defined on integer positions and bounded between zero (inclusive) and <see cref="size"/> (exclusive).
/// </summary>
public abstract partial class TextureGrid : Texture
{
	protected TextureGrid(Int2 size, IFilter filter) : base(Wrappers.repeat)
	{
		Assert.IsTrue(size > Int2.Zero);

		this.size = size;
		sizeR = 1f / size;
		oneLess = size - Int2.One;

		aspect = (float)size.X / size.Y;
		Filter = filter;
	}

	/// <summary>
	/// The size of this <see cref="Texture"/> (exclusive),
	/// </summary>
	public readonly Int2 size;

	/// <summary>
	/// The reciprocal of <see cref="size"/>.
	/// </summary>
	public readonly Float2 sizeR;

	/// <summary>
	/// The <see cref="size"/> of this <see cref="Texture"/> minus one.
	/// </summary>
	public readonly Int2 oneLess;

	/// <summary>
	/// The aspect ratio of this <see cref="Texture"/>, equals to width over height.
	/// </summary>
	public readonly float aspect;

	NotNull<object> _filter;

	/// <summary>
	/// The <see cref="IFilter"/> used on this <see cref="TextureGrid"/> to retrieve pixels.
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
	/// Gets and sets the <see cref="Vector128{T}"/> pixel data at a specific <paramref name="position"/>.
	/// The input <paramref name="position"/> must be between zero and <see cref="oneLess"/> (both inclusive).
	/// </summary>
	public abstract Vector128<float> this[Int2 position] { get; set; }

	protected sealed override Vector128<float> Evaluate(Float2 uv)
	{
		Assert.IsFalse(float.IsNaN(uv.X));
		Assert.IsFalse(float.IsNaN(uv.Y));
		return Filter.Convert(this, uv);
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
	/// Copies as much data from <paramref name="texture"/> to this <see cref="TextureGrid"/> as fast as possible.
	/// </summary>
	public virtual void CopyFrom(Texture texture, bool parallel = true) => ForEach
	(
		texture is TextureGrid grid && grid.size == size ?
			position => this[position] = grid[position] :
			position => this[position] = texture[ToUV(position)], parallel
	);

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

	protected void AssertAlignedSize(TextureGrid texture)
	{
		if (texture.size == size) return;
		throw ExceptionHelper.Invalid(nameof(texture), texture, "has a mismatched size!");
	}

	public override string ToString() => $"{base.ToString()} with size {size}";
}