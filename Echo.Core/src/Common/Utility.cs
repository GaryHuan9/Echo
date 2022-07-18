using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common;

public static class Utility
{
	/// <summary>
	/// Swaps the item held by two references..
	/// </summary>
	/// <param name="first">The first reference to swap from.</param>
	/// <param name="second">The second reference to swap from.</param>
	/// <typeparam name="T">The type of the item to swap.</typeparam>
	public static void Swap<T>(ref T first, ref T second)
	{
		T storage = first;
		first = second;
		second = storage;
	}

	/// <summary>
	/// If <paramref name="index"/> is valid for <paramref name="span"/>, returns
	/// the item it points. Otherwise, <paramref name="defaultValue"/> is returned.
	/// </summary>
	public static ref readonly T TryGetValue<T>(this ReadOnlySpan<T> span, int index, in T defaultValue = default)
	{
		if ((uint)index < span.Length) return ref span[index];
		return ref defaultValue;
	}

	/// <summary>
	/// Tries to retrieve from an <see cref="IReadOnlyDictionary{TKey,TValue}"/>.
	/// </summary>
	/// <param name="dictionary">The <see cref="IReadOnlyDictionary{TKey,TValue}"/> to retrieve from.</param>
	/// <param name="key">The item used to attempt to locate the value in <paramref name="dictionary"/>.</param>
	/// <returns>If found, the value of that was retrieved, otherwise default.</returns>
	public static TValue TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) => dictionary.TryGetValue(key, out TValue value) ? value : default;

	/// <summary>
	/// Unsafe fast memory copy.
	/// </summary>
	/// <param name="source">The origin.</param>
	/// <param name="target">The destination.</param>
	/// <param name="length">The number of items to copy.</param>
	/// <typeparam name="T">The type of item to copy.</typeparam>
	public static unsafe void MemoryCopy<T>(T* source, T* target, long length) where T : unmanaged =>
		Buffer.MemoryCopy(source, target, length * sizeof(T), length * sizeof(T));

	public static float GetScale(in Float4x4 transform) => transform.GetRow(0).XYZ_.Magnitude;

	/// <summary>
	/// Ensures an <see cref="Array"/> is large enough for some number of items.
	/// </summary>
	/// <param name="array">The <see cref="Array"/> to ensure; this argument can be null.</param>
	/// <param name="capacity">The number of items to potentially store.</param>
	/// <param name="copy">Whether to copy over the old items in <paramref name="array"/> to the new one.</param>
	/// <param name="capacityMin">The minimum <see cref="Array.Length"/> if a new <see cref="Array"/> is created.</param>
	public static void EnsureCapacity<T>(ref T[] array, int capacity, bool copy = false, int capacityMin = 4)
	{
		if ((array?.Length ?? 0) >= capacity) return;

		uint length = (uint)Math.Max(capacity, capacityMin);
		length = BitOperations.RoundUpToPowerOf2(length);

		if (!copy || array == null) array = new T[length];
		else Array.Resize(ref array, (int)length);
	}

	/// <summary>
	/// Calculates and returns a deterministic hash code for <paramref name="value"/>.
	/// </summary>
	public static unsafe int GetHashCode<T>(Vector128<T> value) where T : struct => GetHashCode(&value);

	/// <inheritdoc cref="GetHashCode(byte*,uint,int)"/>
	public static unsafe int GetHashCode<T>(T* ptr, uint length = 1, int initial = 0) where T : unmanaged => GetHashCode((byte*)ptr, length * (uint)sizeof(T), initial);

	/// <summary>
	/// Calculates and returns a deterministic hash code from <paramref name="ptr"/> to <paramref name="length"/>.
	/// The entire memory domain defined by the two parameters is scanned, and any change to it will alter the result.
	/// </summary>
	public static unsafe int GetHashCode(byte* ptr, uint length = 1, int initial = 0)
	{
		int hashCode = initial;

		unchecked
		{
			int* intPtr = (int*)ptr - 1;
			uint intLength = length / 4;

			for (uint i = 0; i < intLength; i++) hashCode = (hashCode * 397) ^ *++intPtr;
			for (uint i = intLength * 4; i < length; i++) hashCode = (hashCode * 397) ^ ptr[i];
		}

		return hashCode;
	}

	public static int Morton(Int2 position) => Saw((short)position.X) | (Saw((short)position.Y) << 1); //Uses Morton encoding to improve cache hit chance
	public static Int2 Morton(int index) => new(Unsaw(index), Unsaw(index >> 1));

	/// <summary>
	/// Transforms a number into a saw blade shape:
	/// _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0
	/// _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
	/// </summary>
	static int Saw(short number)
	{
		//NOTE: we can use the pext and pdep instructions under the BMI2 instruction set to accelerate this
		//https://stackoverflow.com/a/30540867/9196958

		int x = number;

		x = (x | (x << 08)) & 0b0000_0000_1111_1111_0000_0000_1111_1111; // _ _ _ _ 7 6 5 4 _ _ _ _ 3 2 1 0
		x = (x | (x << 04)) & 0b0000_1111_0000_1111_0000_1111_0000_1111; // _ _ 7 6 _ _ 5 4 _ _ 3 2 _ _ 1 0
		x = (x | (x << 02)) & 0b0011_0011_0011_0011_0011_0011_0011_0011; // _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
		x = (x | (x << 01)) & 0b0101_0101_0101_0101_0101_0101_0101_0101; // Final step not representable in 8 bit version

		return x;
	}

	/// <summary>
	/// Transforms a saw blade shape number back:
	/// _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
	/// _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0
	/// </summary>
	static short Unsaw(int number)
	{
		int x = number;

		x = (x | (x >> 00)) & 0b0101_0101_0101_0101_0101_0101_0101_0101; // _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
		x = (x | (x >> 01)) & 0b0011_0011_0011_0011_0011_0011_0011_0011; // _ _ 7 6 _ _ 5 4 _ _ 3 2 _ _ 1 0
		x = (x | (x >> 02)) & 0b0000_1111_0000_1111_0000_1111_0000_1111; // _ _ _ _ 7 6 5 4 _ _ _ _ 3 2 1 0
		x = (x | (x >> 04)) & 0b0000_0000_1111_1111_0000_0000_1111_1111; // _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0

		return (short)(x | (x >> 08));
	}
}