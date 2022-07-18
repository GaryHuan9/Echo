using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Threading;

/// <summary>
/// A lockless <see cref="List{T}"/> that supports semi-concurrent read and add operations.
/// To start adding items to this list, invoke <see cref="BeginAdd"/> first, then invoke
/// <see cref="Add"/> to append new items to the end of the list. Remember to invoke
/// <see cref="EndAdd"/> after the adding operations are finished. The state of the list is kept
/// constant while adding, before <see cref="EndAdd"/> is invoked. Removing is not supported.
/// </summary>
public class ConcurrentList<T> : IReadOnlyList<T>
{
	public ConcurrentList() => arrays = new T[31][];

	readonly T[][] arrays;

	int adding; //Whether we are currently adding; this value indicates the number of layers of begin add that has been invoked
	int next;   //The next index to be added, this is only updated after begin add is invoked
	int count;  //The current count of added items, this is updated after end add is invoked

	/// <summary>
	/// Returns the current number of items in this <see cref="ConcurrentList{T}"/>.
	/// </summary>
	public int Count => InterlockedHelper.Read(ref count);

	/// <summary>
	/// Returns whether this <see cref="ConcurrentList{T}"/> is prepared for addition.
	/// </summary>
	public bool Adding => InterlockedHelper.Read(ref adding) > 0;

	/// <summary>
	/// Returns a reference to the item at <paramref name="index"/>.
	/// Note that you can directly modify the item through the reference.
	/// </summary>
	public ref T this[int index]
	{
		get
		{
			if ((uint)index < Count) return ref arrays[GetIndex(index, out int offset)][offset];
			throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
		}
	}

	/// <summary>
	/// Adds <paramref name="item"/> to this <see cref="ConcurrentList{T}"/>. This method must only be invoked after
	/// <see cref="BeginAdd"/> is invoked once and before the equivalent amount of <see cref="EndAdd"/> is invoked.
	/// </summary>
	public void Add(T item)
	{
		if (InterlockedHelper.Read(ref adding) <= 0) throw new Exception($"Cannot invoke {nameof(Add)} before invoking {nameof(BeginAdd)}!");

		int target = Interlocked.Increment(ref next) - 1;
		int index = GetIndex(target, out int offset);

		ref T[] array = ref arrays[index];

		if (array == null) Interlocked.CompareExchange(ref array, new T [1 << index], null);

		Ensure.IsNotNull(array);
		array[offset] = item;
	}

	/// <summary>
	/// Invokes <see cref="Add"/> on every item in <paramref name="items"/>.
	/// </summary>
	public void AddRange(IEnumerable<T> items)
	{
		foreach (T item in items) Add(item);
	}

	/// <summary>
	/// Immediately invoke <see cref="Add"/> without worrying
	/// about invoking <see cref="BeginAdd"/> and <see cref="EndAdd"/>.
	/// </summary>
	public void ImmediateAdd(T item)
	{
		using var _ = BeginAdd();
		Add(item);
	}

	/// <summary>
	/// Immediately invoke <see cref="AddRange"/> without worrying
	/// about invoking <see cref="BeginAdd"/> and <see cref="EndAdd"/>.
	/// </summary>
	public void ImmediateAddRange(IEnumerable<T> items)
	{
		using var _ = BeginAdd();
		AddRange(items);
	}

	/// <summary>
	/// Begins allowing invocations to the <see cref="Add"/> method. This method returns a <see cref="AddHandle"/>,
	/// which allows you to use using statements or expressions to automatically invoke <see cref="EndAdd"/>
	/// after the scope exits. NOTE: supports nested invocations.
	/// </summary>
	public AddHandle BeginAdd()
	{
		Interlocked.Increment(ref adding);
		return new AddHandle(this);
	}

	/// <summary>
	/// Concludes allowing invocations to the <see cref="Add"/> method. NOTE: supports nested invocations.
	/// </summary>
	public void EndAdd()
	{
		int value = Interlocked.Decrement(ref adding);

		if (value < 0) throw new Exception($"Cannot invoke {nameof(EndAdd)} before invoking {nameof(BeginAdd)}!");
		if (value == 0) Interlocked.Exchange(ref count, InterlockedHelper.Read(ref next));
	}

	/// <summary>
	/// Returns the content of this <see cref="ConcurrentList{T}"/> copied to an <see cref="Array"/>.
	/// </summary>
	public T[] ToArray()
	{
		T[] array = new T[Count];

		for (int i = 0; i < array.Length; i++) array[i] = this[i];

		return array;
	}

	public Enumerator GetEnumerator() => new Enumerator(this);

	T IReadOnlyList<T>.this[int index] => this[index];

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	static int GetIndex(int index, out int offset)
	{
		++index;
#if NETCOREAPP3_0_OR_GREATER
		int log = System.Numerics.BitOperations.Log2((uint)index);
#else
			int log = -1;
			uint value = (uint)index;

			while (value > 0)
			{
				value >>= 1;
				++log;
			}
#endif
		offset = index & ~(1 << log);
		return log;
	}

	public struct AddHandle : IDisposable
	{
		public AddHandle(ConcurrentList<T> list)
		{
			this.list = list;
			disposed = false;
		}

		readonly ConcurrentList<T> list;

		bool disposed;

		public void Dispose()
		{
			if (disposed) return;

			list.EndAdd();
			disposed = true;
		}
	}

	public struct Enumerator : IEnumerator<T>
	{
		public Enumerator(ConcurrentList<T> list)
		{
			Current = default;
			this.list = list;
			index = -1;
		}

		public T Current { get; private set; }
		int index;

		readonly ConcurrentList<T> list;

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			int count = list.Count;

			if (index < count) Current = ++index == count ? default : list[index];

			return index < count;
		}

		public void Reset()
		{
			Current = default;
			index = -1;
		}

		public void Dispose() { }
	}
}