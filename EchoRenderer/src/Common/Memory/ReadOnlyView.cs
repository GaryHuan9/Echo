using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Common.Memory;

public readonly struct ReadOnlyView<T>
{
	public ReadOnlyView(T[] array)
	{
		this.array = array;
		start = 0;
		count = array.Length;
	}

	public ReadOnlyView(T[] array, int start, int count)
	{
		Assert.IsTrue(start < array.Length);
		Assert.IsFalse(count + start > array.Length);

		this.array = array;
		this.start = start;
		this.count = count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyView<T> Slice(int offset) =>
		new(array, AssertShift(offset), count - offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyView<T> Slice(int offset, int length) =>
		new(array, AssertShift(offset), length);

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) =>
		new(view.array, view.start, view.count);

	public ReadOnlySpan<T> AsSpan() => this;

	public T this[int index] => array[AssertShift(index)];
	public T this[Index index] => array[AssertShift(index.Value)];
	public ReadOnlyView<T> this[Range range] => Slice(range.Start.Value, range.End.Value - range.Start.Value);

	public bool IsEmpty => count == 0 || array == null;

	public readonly int count;

	readonly T[] array;
	readonly int start;

	/// <summary>
	///     Asserts and Shifts the view array index to the original array index
	///     if the <paramref name="index" /> is less than <see cref="count" />
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int AssertShift(int index)
	{
		Assert.IsTrue(index < count);
		return start + index;
	}
}
