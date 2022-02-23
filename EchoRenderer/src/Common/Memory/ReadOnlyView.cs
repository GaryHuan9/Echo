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
		Length = array.Length;
	}

	public ReadOnlyView(T[] array, int start, int count)
	{
		Assert.IsTrue(start < array.Length);
		Assert.IsFalse(count + start > array.Length);

		this.array = array;
		this.start = start;
		Length = count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyView<T> Slice(int offset) =>
		new(array, AssertShift(offset), Length - offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyView<T> Slice(int offset, int length) =>
		new(array, AssertShift(offset), length);

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) =>
		new(view.array, view.start, view.Length);

	public ReadOnlySpan<T> AsSpan() => this;

	public T this[int index] => array[AssertShift(index)];
	public T this[Index index] => array[AssertShift(index.GetOffset(Length))];
	public ReadOnlyView<T> this[Range range] => Slice(range.Start.Value, range.End.Value - range.Start.Value);

	public bool IsEmpty => Length == 0 || array == null;

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
}