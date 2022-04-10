using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

namespace EchoRenderer.Core.Texturing.Grid;

public static class ArrayGrid
{
	/// <summary>
	/// This is the axis in which <see cref="ArrayGrid{T}.ToPosition(int)"/> is going to move first if the input index is incremented.
	/// </summary>
	public const int MajorAxis = 0;

	/// <summary>
	/// The opposite axis of <see cref="MajorAxis"/>.
	/// </summary>
	public const int MinorAxis = MajorAxis ^ 1;
}

/// <summary>
/// The default <see cref="TextureGrid{T}"/>; stores RGBA color information with 32 bits per channel, supports full float range.
/// </summary>
public class ArrayGrid<T> : TextureGrid<T> where T : IColor<T>
{
	public ArrayGrid(Int2 size) : base(size)
	{
		length = size.Product;
		pixels = new T[length];
	}

	protected readonly int length;
	protected readonly T[] pixels;

	public override T this[Int2 position]
	{
		get => pixels[ToIndex(position)];
		set => pixels[ToIndex(position)] = value;
	}

	/// <summary>
	/// Converts the integer pixel <paramref name="position"/> to an index [0, <see cref="length"/>)
	/// </summary>
	public int ToIndex(Int2 position)
	{
		Assert.IsTrue(Int2.Zero <= position);
		Assert.IsTrue(position < size);

		return position.X + position.Y * size.X;
	}

	/// <summary>
	/// Converts <paramref name="index"/> [0, <see cref="length"/>) to an integer pixel position
	/// </summary>
	public Int2 ToPosition(int index)
	{
		Assert.IsTrue(0 <= index && index < length);
		return new Int2(index % size.X, index / size.X);
	}

	public override void CopyFrom(Texture texture, bool parallel = true)
	{
		if (texture is ArrayGrid<T> array && array.size == size)
		{
			//Span is faster on dotnet core
#if NETCOREAPP
			array.pixels.AsSpan().CopyTo(pixels);
#else
			Array.Copy(array.pixels, pixels, length);
#endif
		}
		else base.CopyFrom(texture, parallel);
	}
}