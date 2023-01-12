using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

public static class InvariantFormat
{
	const string DefaultPattern = "N2";
	const string IntegerPattern = "N0";

	static CultureInfo Culture => CultureInfo.InvariantCulture;

	public static bool TryParse(CharSpan span, out float result) => float.TryParse(span, NumberStyles.Float, Culture, out result);
	public static bool TryParse(CharSpan span, out int result) => int.TryParse(span, NumberStyles.Integer, Culture, out result);
	public static bool TryParse(CharSpan span, out uint result) => uint.TryParse(span, NumberStyles.Integer, Culture, out result);

	public static bool TryParse(CharSpan span, out Float2 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float3 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float4 result) => TryParseFloat(span, out result);

	public static bool TryParse(CharSpan span, out Int2 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int3 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int4 result) => TryParseInt(span, out result);

	public static string ToInvariant(this float value) => value.ToString(DefaultPattern, Culture);
	public static string ToInvariant(this double value) => value.ToString(DefaultPattern, Culture);
	public static string ToInvariant(this int value) => value.ToString(IntegerPattern, Culture);
	public static string ToInvariant(this long value) => value.ToString(IntegerPattern, Culture);
	public static string ToInvariant(this uint value) => value.ToString(IntegerPattern, Culture);
	public static string ToInvariant(this ulong value) => value.ToString(IntegerPattern, Culture);

	public static string ToInvariant(this Float2 value) => value.ToString(DefaultPattern, Culture);
	public static string ToInvariant(this in Float3 value) => value.ToString(DefaultPattern, Culture);
	public static string ToInvariant(this in Float4 value) => value.ToString(DefaultPattern, Culture);

	public static string ToInvariant(this Int2 value) => value.ToString(IntegerPattern, Culture);
	public static string ToInvariant(this in Int3 value) => value.ToString(IntegerPattern, Culture);
	public static string ToInvariant(this in Int4 value) => value.ToString(IntegerPattern, Culture);

	public static string ToInvariant(this TimeSpan value) => value.ToString(@"hh\:mm\:ss\.ff", Culture);
	public static string ToInvariant(this DateTime value) => value.ToString("HH:mm:ss", Culture);

	public static string ToInvariant(this in Guid guid)
	{
		Span<char> span = stackalloc char[80]; //36x2 + a bit extra
		bool formatted = guid.TryFormat(span, out int length, "D");
		Ensure.IsTrue(formatted && length <= span.Length / 2);

		//Convert to upper case
		ReadOnlySpan<char> lower = span[..length];
		Span<char> upper = span[length..(length + length)];
		lower.ToUpperInvariant(upper);
		return new string(upper);
	}

	public static string ToInvariant<T>(this /* in */ T value) where T : struct, IFormattable => value.ToString(DefaultPattern, Culture);

	public static string ToInvariantPercent(this float value) => value.ToString("P1", Culture);
	public static string ToInvariantPercent(this double value) => value.ToString("P2", Culture);

	public static string ToInvariantData(this uint value) => ToInvariantData((ulong)value);

	public static string ToInvariantData(this ulong value)
	{
		//Our internal method requires a size of 7 or larger
		Span<char> span = stackalloc char[7];
		int length = ToInvariantMetricInternal(span, value);

		span[length] = 'B';
		return span[..(length + 1)].ToString();
	}

	public static string ToInvariantShort(this in Guid guid) => $"0x{guid.GetHashCode():X8}";

	/// <inheritdoc cref="ToInvariantMetric(ulong)"/>
	public static string ToInvariantMetric(this uint value) => ToInvariantMetric((ulong)value);

	/// <summary>
	/// Formats <paramref name="value"/> to its abbreviations using metric prefixes.
	/// </summary>
	/// <returns>The formatted <see cref="string"/> (eg. 123.4K, 12.34M, or 1.234G).</returns>
	/// <remarks>The returned <see cref="string"/> will also have a <see cref="string.Length"/> of 6 or shorter.</remarks>
	public static string ToInvariantMetric(this ulong value)
	{
		//Our internal method requires a size of 7 or larger
		Span<char> span = stackalloc char[7];
		int length = ToInvariantMetricInternal(span, value);
		return span[..length].ToString();
	}

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

	static int ToInvariantMetricInternal(Span<char> span, ulong value)
	{
		Ensure.IsTrue(span.Length >= 7);

		const ulong L6 = (ulong)1E18;
		const ulong L5 = (ulong)1E15;
		const ulong L4 = (ulong)1E12;
		const ulong L3 = (ulong)1E9;
		const ulong L2 = (ulong)1E6;
		const ulong L1 = (ulong)1E3;

		return value switch
		{
			>= L6 => Format(span, value, L6, 'E'),
			>= L5 => Format(span, value, L5, 'P'),
			>= L4 => Format(span, value, L4, 'T'),
			>= L3 => Format(span, value, L3, 'G'),
			>= L2 => Format(span, value, L2, 'M'),
			>= L1 => Format(span, value, L1, 'K'),
			_ => FormatDefault(span, value)
		};

		static int Format(Span<char> span, ulong value, ulong level, char suffix)
		{
			//Calculate the two parts
			ulong head = value / level;                        //The integer part
			ulong tail = value / (level / 1000) - head * 1000; //The decimal part

			Ensure.IsTrue(head < 1000);
			Ensure.IsTrue(tail < 1000);

			span = span[..7]; //Our first two formats to spawn should only be 2x 3 characters with a 1 gap in between

			//Format into span
			bool formatted = head.TryFormat(span, /*            */ out int length0, default, Culture)
						   & tail.TryFormat(span[(length0 + 1)..], out int length1, "D3", Culture);

			Ensure.IsTrue(formatted);

			//Restricts length to be <= 6, everything beyond that will be ignored
			int length = Math.Min(length0 + 1 + length1 + 1, 6);

			//Add the separator and the suffix
			span[length0] = '.';
			span[length - 1] = suffix;

			return length;
		}

		static int FormatDefault(Span<char> span, ulong value)
		{
			bool formatted = value.TryFormat(span, out int length, default, Culture);

			Ensure.IsTrue(formatted);
			Ensure.IsTrue(length <= 3);
			return length;
		}
	}
}