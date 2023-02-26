using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Echo.Core.Common.Memory;

/// <summary>
/// Similar to a regular <see cref="Array"/> with type <typeparamref name="T"/>,
/// but everything nicely aligns with the cache line and is always pinned.
/// </summary>
/// <typeparam name="T">The unmanaged type to contain.</typeparam>
public sealed unsafe class AlignedArray<T> : IDisposable where T : unmanaged
{
	/// <summary>
	/// Constructs a new <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <param name="length">The number of items to contain.</param>
	/// <param name="clear">Whether to initialize the content of this <see cref="AlignedArray{T}"/> to all zeros.</param>
	/// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> cannot be nicely aligned with the cache line.</exception>
	public AlignedArray(int length, bool clear = true)
	{
		if ((sizeof(T) > CacheWidth && sizeof(T) % CacheWidth > 0) ||
			(sizeof(T) < CacheWidth && CacheWidth % sizeof(T) > 0))
			throw new ArgumentException($"Cannot aligned allocate type `{typeof(T)}` because its size is {sizeof(T)}.");

		nuint byteCount = (nuint)((long)length * sizeof(T));
		Pointer = (T*)NativeMemory.AlignedAlloc(byteCount, CacheWidth);

		Length = length;
		if (clear && length > 0) Clear();
	}

	/// <summary>
	/// Constructs a new <see cref="AlignedArray{T}"/> from a <see cref="ReadOnlySpan{T}"/>.
	/// </summary>
	/// <param name="source">The original memory to be copied to this new <see cref="AlignedArray{T}"/>.</param>
	/// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> cannot be nicely aligned with the cache line.</exception>
	public AlignedArray(ReadOnlySpan<T> source) : this(source.Length, false) => source.CopyTo(new Span<T>(Pointer, Length));

	/// <summary>
	/// The width of the cache line in bytes. This value is basically always 64 bytes for any modern computers.
	/// </summary>
	const uint CacheWidth = 64;

	/// <summary>
	/// The pointer to the first element in this <see cref="AlignedArray{T}"/>. Once
	/// <see cref="Dispose"/> has been invoked this property becomes null permanently.
	/// </summary>
	public T* Pointer { get; private set; }

	/// <summary>
	/// The number of elements in this <see cref="AlignedArray{T}"/>. Once
	/// <see cref="Dispose"/> has been invoked this property becomes zero permanently.
	/// </summary>
	public int Length { get; private set; }

	/// <inheritdoc cref="Item(uint)"/>
	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref this[(uint)index];
	}

	/// <summary>
	/// Accesses an item in this <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <param name="index">The zero-based numerical index of the item to access.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is
	/// negative or larger than or equals to <see cref="Length"/>.</exception>
	/// <remarks>If <paramref name="index"/> is out of bounds, the behavior
	/// in RELEASE mode is undefined for performance reasons.</remarks>
	public ref T this[uint index]
	{
#if RELEASE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref Pointer[index];
#else
		get
		{
			if (index < Length) return ref Pointer[index];
			throw new IndexOutOfRangeException(nameof(index));
#endif
		}
	}

	/// <summary>
	/// Sets all the bits contained in this <see cref="AlignedArray{T}"/> to zero.
	/// </summary>
	public void Clear() => AsSpan().Clear();

	/// <summary>
	/// Creates a <see cref="Span{T}"/> over this <see cref="AlignedArray{T}"/>.
	/// </summary>
	/// <returns>The <see cref="Span{T}"/> that was created.</returns>
	public Span<T> AsSpan() => new(Pointer, Length);

	public void Dispose()
	{
		ReleaseUnmanagedMemory();
		GC.SuppressFinalize(this);
	}

	void ReleaseUnmanagedMemory()
	{
		if (Pointer == null) return;
		NativeMemory.AlignedFree(Pointer);

		Pointer = null;
		Length = 0;
	}

	~AlignedArray() => ReleaseUnmanagedMemory();

	public static implicit operator Span<T>(AlignedArray<T> array) => array.AsSpan();
}