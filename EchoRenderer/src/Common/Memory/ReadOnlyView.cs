using System;
using System.Runtime.CompilerServices;

namespace EchoRenderer.Common.Memory;

public readonly struct ReadOnlyView<T>
{
	public ReadOnlyView(T[] array)
	{
		this.array = array;
		shift = 0;
		Count = array.Length;
	}

	public ReadOnlyView(T[] array, int start, int count)
	{
		this.array = array;
		shift = start;
		Count = count;
	}

	public ReadOnlyView<T> Slice(int offset) => throw new NotImplementedException();
	public ReadOnlyView<T> Slice(int offset, int length) => throw new NotImplementedException();

	public static implicit operator ReadOnlySpan<T>(ReadOnlyView<T> view) =>
		new ReadOnlySpan<T>(view.array, view.shift, view.Count);

	public T this[int index] => array[IndexShift(index)];

	public T this[Index index] => array[IndexShift(index.Value)];

	public ReadOnlyView<T> this[Range range] => throw new NotImplementedException();

	public bool IsEmpty => Count == 0 || array == null;

	public int Count { get; }

	readonly T[] array;
	readonly int shift;

	/// <summary>
	///     Shifts the view array index to the original array index
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int IndexShift(int index) => index + shift;
}
