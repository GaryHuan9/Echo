using System;
using System.Runtime.CompilerServices;

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
		this.array = array;
		this.start = start;
		this.count = count;
	}

	public void Clear() => throw new NotImplementedException();

	public View<T> Slice(int offset) => throw new NotImplementedException();
	public View<T> Slice(int offset, int length) => throw new NotImplementedException();

	public static implicit operator ReadOnlyView<T>(View<T> view) =>
		new(view.array, view.start, view.count);
	public static implicit operator Span<T>(View<T> view) =>
		new(view.array, view.start, view.count);
	public static implicit operator ReadOnlySpan<T>(View<T> view) =>
		new(view.array, view.start, view.count);

	public T this[int index]
	{
		get => array[IndexShift(index)];
		set => array[IndexShift(index)] = value;
	}

	public T this[Index index]
	{
		get => array[IndexShift(index.Value)];
		set => array[IndexShift(index.Value)] = value;
	}

	public View<T> this[Range range]
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	public bool IsEmpty => count == 0 || array == null;

	public readonly int count;

	readonly T[] array;
	readonly int start;

	/// <summary>
	///     Shifts the view array index to the original array index
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int IndexShift(int index) => index + start;
}
