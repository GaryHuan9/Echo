using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Packed;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

public static class Parsers
{
	public static bool TryParse(CharSpan span, out float result) => float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	public static bool TryParse(CharSpan span, out int result) => int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

	public static bool TryParse(CharSpan span, out Float2 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float3 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float4 result) => TryParseFloat(span, out result);

	public static bool TryParse(CharSpan span, out Int2 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int3 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int4 result) => TryParseInt(span, out result);

	static bool TryParseFloat<T>(CharSpan span, out T result) where T : unmanaged
	{
		Span<float> destination = ToSpan<T, float>(out result);

		for (int i = 0; i < destination.Length - 1; i++)
		{
			int index = FindWhiteSpace(span);
			CharSpan slice = span[..index];

			if (!TryParse(slice, out destination[i])) return false;
			span = span[(index + 1)..];
		}

		return TryParse(span, out destination[^1]);
	}

	static bool TryParseInt<T>(CharSpan span, out T result) where T : unmanaged
	{
		Span<int> destination = ToSpan<T, int>(out result);

		for (int i = 0; i < destination.Length - 1; i++)
		{
			int index = FindWhiteSpace(span);
			CharSpan slice = span[..index];

			if (!TryParse(slice, out destination[i])) return false;
			span = span[(index + 1)..];
		}

		return TryParse(span, out destination[^1]);
	}

	[SkipLocalsInit]
	static unsafe Span<TTo> ToSpan<TFrom, TTo>(out TFrom result) where TFrom : unmanaged
																 where TTo : unmanaged
	{
		Unsafe.SkipInit(out result);
		int length = sizeof(TFrom) / sizeof(TTo);
		ref TTo converted = ref Unsafe.As<TFrom, TTo>(ref result);
		return MemoryMarshal.CreateSpan(ref converted, length);
	}

	static int FindWhiteSpace(CharSpan span)
	{
		int index = 0;

		for (; index < span.Length; index++)
		{
			if (char.IsWhiteSpace(span[index])) break;
		}

		return index;
	}
}