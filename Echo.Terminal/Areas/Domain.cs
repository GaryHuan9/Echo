using System;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Terminal.Areas;

public readonly struct Domain : IEquatable<Domain>
{
	public Domain(Int2 size) : this(size, new char[GetArrayLength(size)]) { }

	Domain(Int2 size, char[] array) : this(size, Int2.Zero, size, array) { }

	Domain(in Domain source, Int2 min, Int2 max) : this
	(
		max - min, min + source.origin,
		source.realSize, source.array
	) { }

	Domain(Int2 size, Int2 origin, Int2 realSize, char[] array)
	{
		Assert.IsTrue(size >= Int2.Zero);
		Assert.IsTrue(realSize >= size);
		Assert.IsTrue((array?.Length ?? 0) >= realSize.Product);

		Assert.IsTrue(origin >= Int2.Zero);
		Assert.IsTrue(realSize >= origin + size);

		this.size = size;
		this.origin = origin;
		this.realSize = realSize;
		this.array = array;
	}

	public readonly Int2 size;
	readonly Int2 origin;
	readonly Int2 realSize;
	readonly char[] array;

	public bool IsRoot => size == realSize;

	public ref char this[Int2 position] => ref this[position.X, position.Y];

	public ref char this[int x, int y]
	{
		get
		{
			Assert.IsTrue(new Int2(x, y) >= Int2.Zero);
			Assert.IsTrue(new Int2(x, y) < size);

			x += origin.X;
			y = realSize.Y - 1 - y - origin.Y;
			return ref array[realSize.X * y + x];
		}
	}

	public Span<char> this[int y] => MemoryMarshal.CreateSpan(ref this[0, y], size.X);

	public Domain this[Int2 min, Int2 max] => new(this, min, max);

	public void Fill(char value = ' ') => FillAbove(0, value);

	public void FillAbove(int y, char value = ' ')
	{
		for (; y < size.Y; y++) FillLine(y, value);
	}

	public void FillBelow(int y, char value = ' ')
	{
		for (; y >= 0; y--) FillLine(y, value);
	}

	public void FillLine(int y, char value = ' ') => this[y].Fill(value);

	public void WriteLine(int y, ReadOnlySpan<char> value)
	{
		Span<char> line = this[y];
		value.CopyTo(line);
		line[value.Length..].Fill(' ');
	}

	public Domain Resize(Int2 newSize)
	{
		if (!IsRoot) throw new InvalidOperationException();

		int current = array.Length;
		int target = GetArrayLength(newSize);

		if (target <= current) return new Domain(newSize, array);

		do current *= 2;
		while (current < target);

		return new Domain(newSize, new char[current]);
	}

	public void DrawToConsole() => Console.Write(array, 0, GetArrayLength(size));

	public bool Equals(in Domain other) => (size == other.size) & (origin == other.origin) &
										   (realSize == other.realSize) & (array == other.array);

	public override bool Equals(object obj) => obj is Domain other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(size, origin, realSize, array);

	bool IEquatable<Domain>.Equals(Domain other) => Equals(other);

	public static bool operator ==(in Domain value, in Domain other) => value.Equals(other);
	public static bool operator !=(in Domain value, in Domain other) => !value.Equals(other);

	static int GetArrayLength(Int2 size) => size.Product;
}