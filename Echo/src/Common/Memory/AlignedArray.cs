using System;
using System.Runtime.InteropServices;

namespace Echo.Common.Memory;

/// <summary>
/// Similar to a regular <see cref="Array"/> with type <typeparamref name="T"/>,
/// but everything nicely aligns with the cache line and are always pinned.
/// </summary>
/// <typeparam name="T">The unmanaged type to contain.</typeparam>
public sealed unsafe class AlignedArray<T> : IDisposable where T : unmanaged
{
	/// <summary>
	/// Constructs a new <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <param name="length">The number of items to contain.</param>
	/// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> cannot be nicely aligned with the cache line.</exception>
	public AlignedArray(int length)
	{
		if ((sizeof(T) > CacheWidth && sizeof(T) % CacheWidth > 0) ||
			(sizeof(T) < CacheWidth && CacheWidth % sizeof(T) > 0))
			throw new ArgumentException($"Cannot aligned allocate type `{typeof(T)}` because its size is {sizeof(T)}.");

		Length = length;

		if (length > 0)
		{
			nuint byteCount = (nuint)((long)length * sizeof(T));
			array = (T*)NativeMemory.AlignedAlloc(byteCount, CacheWidth);
			Clear();
		}
		else array = null;
	}

	readonly T* array;

	/// <summary>
	/// The width of the cache line in bytes. This value is basically always 64 bytes for any modern computers.
	/// </summary>
	const uint CacheWidth = 64;

	/// <summary>
	/// The number of elements in this <see cref="AlignedArray{T}"/>. Once
	/// <see cref="Dispose"/> has been invoked this property becomes zero permanently.
	/// </summary>
	public int Length { get; private set; }

	/// <summary>
	/// The pointer to the first element in this <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Length"/> is zero.</exception>
	public T* Pointer
	{
		get
		{
			if (Length > 0) return array;
			throw new InvalidOperationException();
		}
	}

	/// <summary>
	/// Accesses an item in this <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <param name="index">The zero-based numerical index of the item to access.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is
	/// negative or larger than or equals to <see cref="Length"/>.</exception>
	public ref T this[int index]
	{
		get
		{
			if ((uint)index < Length) return ref array[index];
			throw new IndexOutOfRangeException(nameof(index));
		}
	}

	/// <summary>
	/// Sets all the bits contained in this <see cref="AlignedArray{T}"/> to zero.
	/// </summary>
	public void Clear() => new Span<T>(array, Length).Clear();

	public void Dispose()
	{
		ReleaseUnmanagedMemory();
		GC.SuppressFinalize(this);
	}

	void ReleaseUnmanagedMemory()
	{
		if (Length == 0) return;
		NativeMemory.AlignedFree(array);
		Length = 0;
	}

	~AlignedArray() => ReleaseUnmanagedMemory();
}