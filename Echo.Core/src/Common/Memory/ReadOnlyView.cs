using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;

namespace Echo.Core.Common.Memory;

/// <summary>
/// A readonly version of <see cref="View{T}"/> that only supports read operations.
/// </summary>
public readonly struct ReadOnlyView<T>
{
	public ReadOnlyView(T[] array, int start = 0) : this(array, start, array.Length - start) { }

	public ReadOnlyView(T[] array, int start, int count)
	{
		Assert.IsNotNull(array);
		Assert.IsFalse((ulong)(uint)start + (uint)count > (uint)array.Length);

		this.array = array;
		this.start = start;
		Length = count;
	}

	/// <summary>
	///	Returns an empty <see cref="ReadOnlyView{T}"/> object.
	/// </summary>
	public static ReadOnlyView<T> Empty => default;

	/// <summary>
	/// Returns a readonly reference to the specified element of the View.
	/// </summary>
	public ref readonly T this[int index] => ref Unsafe.Add(ref array[0], AssertShift(index));

	/// <summary>
	///	Returns a value which indicates whether this view is empty.
	/// </summary>
	public bool IsEmpty => Length == 0;

	/// <summary>
	///	Returns the length of the current view.
	/// </summary>
	public int Length { get; }

	readonly T[] array;
	readonly int start;

	/// <summary>
	/// Returns an enumerator for this View.
	/// </summary>
	public ReadOnlySpan<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

	/// <summary>
	///	Forms a slice out of the given readonly view, beginning at '<paramref name="offset"/>'.
	/// </summary>
	public ReadOnlyView<T> Slice(int offset) => Slice(offset, Length - offset);

	/// <inheritdoc cref="Slice(int)"/>
	public ReadOnlyView<T> Slice(int offset, int length) => new(array, AssertShift(offset), length);

	/// <summary>
	/// Converts the current view into a <see cref="ReadOnlySpan{T}"/>.
	/// </summary>
	public ReadOnlySpan<T> AsSpan() => array.AsSpan(start, Length);

	/// <inheritdoc cref="AsSpan()"/>
	public ReadOnlySpan<T> AsSpan(int offset) => array.AsSpan(AssertShift(offset), Length);

	/// <inheritdoc cref="AsSpan()"/>
	public ReadOnlySpan<T> AsSpan(int offset, int length) => array.AsSpan(AssertShift(offset), length);

	/// <inheritdoc cref="AsSpan()"/>
	public ReadOnlySpan<T> AsSpan(Range range)
	{
		(int offset, int length) = range.GetOffsetAndLength(Length);
		return AsSpan(offset, length);
	}

	/// <inheritdoc cref="AsSpan()"/>
	public ReadOnlySpan<T> AsSpan(Index startIndex) => AsSpan(startIndex.GetOffset(Length));

	/// <summary>
	/// Asserts and Shifts the view array index to the original array index,
	/// if the <paramref name="index"/> is not greater than <see cref="Length"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int AssertShift(int index)
	{
		Assert.IsFalse((uint)index > Length);
		return start + index;
	}

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator ReadOnlyView<T>(T[] array) => new(array);
}