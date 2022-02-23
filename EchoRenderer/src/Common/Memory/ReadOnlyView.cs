using System;
using System.Runtime.CompilerServices;

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
		this.array = array;
		this.start = start;
		this.count = count;
	}

	public ReadOnlyView<T> Slice(int offset) => throw new NotImplementedException();
	public ReadOnlyView<T> Slice(int offset, int length) => throw new NotImplementedException();

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) =>
		new(view.array, view.start, view.count);

	public T this[int index] => array[IndexShift(index)];

	public T this[Index index] => array[IndexShift(index.Value)];

	public ReadOnlyView<T> this[Range range] => throw new NotImplementedException();

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
