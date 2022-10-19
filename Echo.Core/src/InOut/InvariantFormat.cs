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
	public const string FloatingFormat = "N2";
	public const string IntegerFormat = "N0";

	public static bool TryParse(CharSpan span, out float result) => float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	public static bool TryParse(CharSpan span, out int result) => int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

	public static bool TryParse(CharSpan span, out Float2 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float3 result) => TryParseFloat(span, out result);
	public static bool TryParse(CharSpan span, out Float4 result) => TryParseFloat(span, out result);

	public static bool TryParse(CharSpan span, out Int2 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int3 result) => TryParseInt(span, out result);
	public static bool TryParse(CharSpan span, out Int4 result) => TryParseInt(span, out result);

	public static string ToInvariant(this float value) => value.ToString(FloatingFormat);
	public static string ToInvariant(this double value) => value.ToString(FloatingFormat);
	public static string ToInvariant(this int value) => value.ToString(IntegerFormat);
	public static string ToInvariant(this long value) => value.ToString(IntegerFormat);
	public static string ToInvariant(this uint value) => value.ToString(IntegerFormat);
	public static string ToInvariant(this ulong value) => value.ToString(IntegerFormat);

	public static string ToInvariant(this TimeSpan value) => value.ToString(@"hh\:mm\:ss\.ff");
	public static string ToInvariant(this DateTime value) => value.ToString("HH:mm:ss");

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

	public static string ToInvariantPercent(this float value) => value.ToString("P1");
	public static string ToInvariantPercent(this double value) => value.ToString("P2");

	public static string ToInvariantData(this uint value) => value.ToInvariantMetric() + 'B';
	public static string ToInvariantData(this ulong value) => value.ToInvariantMetric() + 'B';

	public static string ToInvariantShort(this in Guid guid) => $"0x{guid.GetHashCode():X8}";

	/// <summary>
	/// Formats <paramref name="value"/> to its abbreviations using metric prefixes.
	/// </summary>
	/// <returns>The formatted <see cref="string"/> (eg. 123.4K, 12.34M, or 1.234G).</returns>
	/// <remarks>The returned <see cref="string"/> will also have a <see cref="string.Length"/> of 6 or shorter.</remarks>
	public static string ToInvariantMetric(this uint value, IFormatProvider provider = null)
	{
		const uint L3 = (uint)1E9;
		const uint L2 = (uint)1E6;
		const uint L1 = (uint)1E3;

		return value switch
		{
			>= L3 => Format(L3, 'G'),
			>= L2 => Format(L2, 'M'),
			>= L1 => Format(L1, 'K'),
			_ => value.ToString()
		};

		string Format(uint level, char suffix)
		{
			//Calculate the two parts
			uint head = value / level;                        //The integer part
			uint tail = value / (level / 1000) - head * 1000; //The decimal part

			return MergeMetricParts(head, tail, suffix);
		}
	}

	/// <inheritdoc cref="ToInvariantMetric"/>
	public static string ToInvariantMetric(this ulong value, IFormatProvider provider = null)
	{
		const ulong L6 = (ulong)1E18;
		const ulong L5 = (ulong)1E15;
		const ulong L4 = (ulong)1E12;
		const ulong L3 = (ulong)1E9;
		const ulong L2 = (ulong)1E6;
		const ulong L1 = (ulong)1E3;

		return value switch
		{
			>= L6 => Format(L6, 'E'),
			>= L5 => Format(L5, 'P'),
			>= L4 => Format(L4, 'T'),
			>= L3 => Format(L3, 'G'),
			>= L2 => Format(L2, 'M'),
			>= L1 => Format(L1, 'K'),
			_ => value.ToString()
		};

		string Format(ulong level, char suffix)
		{
			//Calculate the two parts
			ulong head = value / level;                        //The integer part
			ulong tail = value / (level / 1000) - head * 1000; //The decimal part

			return MergeMetricParts((uint)head, (uint)tail, suffix);
		}
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

	static string MergeMetricParts(uint head, uint tail, char suffix)
	{
		//Create buffer, 123.456X is the maximum length
		Span<char> span = stackalloc char[8];
		int length = MergeMetricParts(span, head, tail, suffix);
		return new string(span[..length]);
	}

	static int MergeMetricParts(Span<char> span, uint head, uint tail, char suffix)
	{
		Ensure.IsTrue(head < 1000);
		Ensure.IsTrue(tail < 1000);

		//Format into span
		bool formatted = head.TryFormat(span, /*            */ out int length0)
					   & tail.TryFormat(span[(length0 + 1)..], out int length1, "D3");

		Ensure.IsTrue(formatted);

		//Add the separator and the suffix
		int length = Math.Min(length0 + 1 + length1 + 1, 6);

		span[length0] = '.';
		span[length - 1] = suffix;

		return length;
	}
}