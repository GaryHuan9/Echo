using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Common.Memory;

public readonly struct ReadOnlyView<T>
{
	public ReadOnlyView(T[] array, int start = 0) : this(array, start, array.Length - start) { }

	public ReadOnlyView(T[] array, int start, int count)
	{
		Assert.IsNotNull(array);
		Assert.IsTrue(start < array.Length);
		Assert.IsFalse(count + start > array.Length);

		this.array = array;
		this.start = start;
		Length = count;
	}

	public ReadOnlySpan<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

	public ReadOnlyView<T> Slice(int offset) => Slice(offset, Length - offset);

	public ReadOnlyView<T> Slice(int offset, int length) => new(array, AssertShift(offset), length);

	public ReadOnlySpan<T> AsSpan() => this;
	public ReadOnlySpan<T> AsSpan(int offset) => this[offset..];
	public ReadOnlySpan<T> AsSpan(int offset, int length) => Slice(offset, length);

	public static ReadOnlyView<T> Empty => default;

	public ref readonly T this[int index] => ref array[AssertShift(index)];

	public bool IsEmpty => Length == 0;

	public int Length { get; }

	readonly T[] array;
	readonly int start;

	/// <summary>
	///     Asserts and Shifts the view array index to the original array index
	///     if the <paramref name="index" /> is less than <see cref="Length" />
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int AssertShift(int index)
	{
		Assert.IsTrue(index < Length);
		return start + index;
	}

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator ReadOnlyView<T>(T[] array) => new(array);
}
