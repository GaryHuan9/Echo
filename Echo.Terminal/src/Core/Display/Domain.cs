using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Terminal.Core.Display;

public readonly struct Domain
{
	public Domain(Int2 size) : this(size, new char[GetArrayLength(size)])
	{
		if (!(size > Int2.Zero)) throw new ArgumentOutOfRangeException(nameof(size));
	}

	Domain(Int2 size, char[] array)
	{
		Assert.IsTrue(size >= Int2.Zero);
		Assert.IsTrue(array.Length >= size.Product);

		this.size = size;
		this.array = array;
	}

	public readonly Int2 size;
	readonly char[] array;

	public Domain Resize(Int2 newSize)
	{
		if (!(newSize > Int2.Zero)) throw new ArgumentOutOfRangeException(nameof(newSize));
		if ((array?.Length ?? 0) == 0) return new Domain(newSize);

		int current = array.Length;
		int length = GetArrayLength(newSize);

		if (length <= current) return new Domain(newSize, array);

		do current *= 2;
		while (current < length);

		return new Domain(newSize, new char[current]);
	}

	public Painter MakePainter(Int2 min, Int2 max, bool invertY = false)
	{
		int stride = GetStride(invertY);
		int offset = GetOffset(invertY, min);
		return new Painter(max - min, array, stride, offset);
	}

	public void CopyToConsole() => Console.Write(array, 0, GetArrayLength(size));

	int GetStride(bool invertY) => invertY ? -size.X : size.X;

	int GetOffset(bool invertY, Int2 min)
	{
		int offsetY = invertY ? size.Y - min.Y - 1 : min.Y;
		return size.X * offsetY + min.X;
	}

	static int GetArrayLength(Int2 size) => size.Product;
}