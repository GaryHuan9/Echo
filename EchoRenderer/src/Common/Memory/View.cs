using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Common.Memory;

public readonly struct View<T>
{
	public View(T[] array)
	{
		this.array = array;
		start = 0;
		count = array.Length;
	}

	public View(T[] array, int start, int count)
	{
		Assert.IsTrue(start < array.Length);
		Assert.IsFalse(count + start > array.Length);

		this.array = array;
		this.start = start;
		this.count = count;
	}

	public void Clear() => throw new NotImplementedException();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public View<T> Slice(int offset) =>
		new(array, AssertShift(offset), count - offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public View<T> Slice(int offset, int length) =>
		new(array, AssertShift(offset), length);

	public static implicit operator ReadOnlyView<T>(View<T> view) =>
		new(view.array, view.start, view.count);
	public static implicit operator Span<T>(View<T> view) =>
		new(view.array, view.start, view.count);
	public static implicit operator ReadOnlySpan<T>(View<T> view) =>
		new(view.array, view.start, view.count);

	public Span<T> AsSpan() => this;

	public T this[int index]
	{
		get => array[AssertShift(index)];
		set => array[AssertShift(index)] = value;
	}

	public T this[Index index]
	{
		get => array[AssertShift(index.GetOffset(count))];
		set => array[AssertShift(index.GetOffset(count))] = value;
	}

	public View<T> this[Range range]
	{
		get => Slice(range.Start.Value, range.End.Value - range.Start.Value);
		set => throw new NotImplementedException();
	}

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
public static class ViewExtensions
{
	public static View<T> AsView<T>(this T[] array) => new(array);
}
