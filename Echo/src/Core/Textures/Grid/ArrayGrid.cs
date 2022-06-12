using System;
using System.ComponentModel;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grid;

/// <summary>
/// The default <see cref="TextureGrid{T}"/> implemented with a contiguous array.
/// </summary>
public class ArrayGrid<T> : SettableGrid<T> where T : unmanaged, IColor<T>
{
	public ArrayGrid(Int2 size) : base(size)
	{
		length = size.Product;
		pixels = new T[length];
	}

	/// <summary>
	/// The number of pixels in this <see cref="ArrayGrid{T}"/>.
	/// </summary>
	protected readonly int length;

	/// <summary>
	/// The underlying pixel storage of this <see cref="ArrayGrid{T}"/>.
	/// </summary>
	protected readonly T[] pixels;

	/// <summary>
	/// This is the axis in which <see cref="ToPosition"/> is going to move first if the input index is incremented.
	/// </summary>
	public const int MajorAxis = 0;

	/// <summary>
	/// The opposite axis of <see cref="MajorAxis"/>.
	/// </summary>
	public const int MinorAxis = MajorAxis ^ 1;

	public override T this[Int2 position] => pixels[ToIndex(position)];

	public override void Set(Int2 position, in T value) => pixels[ToIndex(position)] = value;

	public override unsafe void CopyFrom(Texture texture)
	{
		if (texture is ArrayGrid<T> array && array.size == size)
		{
			fixed (T* source = array)
			fixed (T* target = this)
			{
				Utility.MemoryCopy(source, target, length);
			}
		}
		else base.CopyFrom(texture);
	}

	/// <summary>
	/// Converts the integer pixel <paramref name="position"/> to an index [0, <see cref="length"/>)
	/// </summary>
	public int ToIndex(Int2 position)
	{
		AssertValidPosition(position);
		return position.X + position.Y * size.X;
	}

	/// <summary>
	/// Converts a pixel <paramref name="index"/> to an integral pixel position.
	/// </summary>
	/// <param name="index">The index of the pixel, must be between 0 (inclusive) and <see cref="length"/> (exclusive).</param>
	public Int2 ToPosition(int index)
	{
		Assert.IsTrue(0 <= index && index < length);
		return new Int2(index % size.X, index / size.X);
	}

	public override void Clear() => Array.Clear(pixels);

	/// <summary>
	/// Implements the pattern-based fixed statement context for `fixed` statements. See the following link for more:
	/// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/pattern-based-fixed
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public ref readonly T GetPinnableReference() => ref pixels[0];
}