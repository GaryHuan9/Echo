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

	public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

	public void Clear() => throw new NotImplementedException();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public View<T> Slice(int offset) => Slice(offset, Length - offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public View<T> Slice(int offset, int length) => new(array, AssertShift(offset), length);

	public Span<T> AsSpan() => this;
	public Span<T> AsSpan(int offset) => this[offset..];
	public Span<T> AsSpan(int offset, int length) => Slice(offset, length);

	public ref T this[int index] => ref array[AssertShift(index)];

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

	public static implicit operator ReadOnlyView<T>(View<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator ReadOnlySpan<T>(View<T> view) => new(view.array, view.start, view.Length);
	public static implicit operator Span<T>(View<T> view) => new(view.array, view.start, view.Length);
}
public static class ViewExtensions
{
	//REMOVE COMMENT: added overloads to allow for slicing and converting with the same method call
	public static View<T> AsView<T>(this T[] array, int start = 0) => new(array, start);
	public static View<T> AsView<T>(this T[] array, int start, int length) => new(array, start, length);
}
