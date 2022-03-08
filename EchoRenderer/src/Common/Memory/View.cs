using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Common.Memory;

public readonly struct View<T>
{
	public View(T[] array, int start = 0) : this(array, start, array.Length - start) { }

	public View(T[] array, int start, int count)
	{
		Assert.IsNotNull(array);
		Assert.IsTrue(start < array.Length);
		Assert.IsFalse(count + start > array.Length);

		this.array = array;
		this.start = start;
		Length = count;
	}

	/// <summary>
	///		Returns an empty <see cref="View{T}"/> object
	/// </summary>
	public static View<T> Empty => default;

	public ref T this[int index] => ref array[AssertShift(index)];

	/// <summary>
	///		Returns a value which indicates whether this view is empty
	/// </summary>
	public bool IsEmpty => Length == 0;

	/// <summary>
	///		Represents the length of the current view 
	/// </summary>
	public int Length { get; }

	readonly T[] array;
	readonly int start;

	public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

	/// <summary>
	///		Forms a slice out of the given view, beginning at '<paramref name="offset"/>'
	/// </summary>
	public View<T> Slice(int offset) => Slice(offset, Length - offset);

	/// <inheritdoc cref="Slice(int)"/>
	public View<T> Slice(int offset, int length) => new(array, AssertShift(offset), length);

	public Span<T> AsSpan() => array.AsSpan(start, Length);
	public Span<T> AsSpan(int offset) => array.AsSpan(AssertShift(offset));
	public Span<T> AsSpan(int offset, int length) => array.AsSpan(AssertShift(offset), length);

	public Span<T> AsSpan(Range range)
	{
		(int offset, int length) = range.GetOffsetAndLength(Length);
		return AsSpan(offset, length);
	}

	public Span<T> AsSpan(Index startIndex) => AsSpan(startIndex.GetOffset(Length));

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

	public static implicit operator ReadOnlyView<T>(View<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator ReadOnlySpan<T>(View<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator Span<T>(View<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator View<T>(T[] array) => new(array);
}
public static class ViewExtensions
{
	public static View<T> AsView<T>(this T[] array, int start = 0) => new(array, start);
	public static View<T> AsView<T>(this T[] array, int start, int length) => new(array, start, length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static View<T> AsView<T>(this T[] array, Range range)
	{
		(int offset, int length) = range.GetOffsetAndLength(array.Length);

		return array.AsView(offset, length);
	}

	public static View<T> AsView<T>(this T[] array, Index index) => array.AsView(index.GetOffset(array.Length));
}