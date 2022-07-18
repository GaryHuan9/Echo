using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Memory;

/// <summary>
/// A sectional view into an array of <typeparamref name="T"/>.
/// </summary>
public readonly struct View<T>
{
	public View(T[] array, int start = 0) : this(array, start, array.Length - start) { }

	public View(T[] array, int start, int count)
	{
		Ensure.IsNotNull(array);
		Ensure.IsFalse((ulong)(uint)start + (uint)count > (uint)array.Length);

		this.array = array;
		this.start = start;
		Length = count;
	}

	readonly T[] array;
	readonly int start;

	/// <summary>
	///	Represents the length of the current view .
	/// </summary>
	public int Length { get; }

	/// <summary>
	/// Returns a reference to the specified element of the View.
	/// </summary>
	public ref T this[int index] => ref Unsafe.Add(ref array[0], EnsureShift(index));

	/// <summary>
	///	Returns a value which indicates whether this view is empty.
	/// </summary>
	public bool IsEmpty => Length == 0;

	/// <summary>
	///	Returns an empty <see cref="View{T}"/> object.
	/// </summary>
	public static View<T> Empty => default;

	/// <summary>
	/// Returns an enumerator for this View.
	/// </summary>
	public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

	/// <summary>
	///	Forms a slice out of the given view, beginning at '<paramref name="offset"/>'.
	/// </summary>
	public View<T> Slice(int offset) => Slice(offset, Length - offset);

	/// <inheritdoc cref="Slice(int)"/>
	public View<T> Slice(int offset, int length) => new(array, EnsureShift(offset), length);

	/// <summary>
	/// Converts the current view into a <see cref="Span{T}"/>.
	/// </summary>
	public Span<T> AsSpan() => array.AsSpan(start, Length);

	/// <inheritdoc cref="AsSpan()"/>
	public Span<T> AsSpan(int offset) => array.AsSpan(EnsureShift(offset));

	/// <inheritdoc cref="AsSpan()"/>
	public Span<T> AsSpan(int offset, int length) => array.AsSpan(EnsureShift(offset), length);

	/// <inheritdoc cref="AsSpan()"/>
	public Span<T> AsSpan(Range range)
	{
		(int offset, int length) = range.GetOffsetAndLength(Length);
		return AsSpan(offset, length);
	}

	/// <inheritdoc cref="AsSpan()"/>
	public Span<T> AsSpan(Index startIndex) => AsSpan(startIndex.GetOffset(Length));

	/// <summary>
	/// Ensures and Shifts the view array index to the original array index,
	/// if the <paramref name="index"/> is not greater than <see cref="Length"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int EnsureShift(int index)
	{
		Ensure.IsFalse((uint)index > Length);
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