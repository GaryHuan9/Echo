using System;
using System.Globalization;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace Echo.UserInterface.Core.Common;

public static class DefaultFormat
{
	public const string Floating = "N2";
	public const string Integer = "N0";

	public static string ToStringDefault(this float value) => value.ToString(Floating);
	public static string ToStringDefault(this double value) => value.ToString(Floating);
	public static string ToStringDefault(this int value) => value.ToString(Integer);
	public static string ToStringDefault(this long value) => value.ToString(Integer);
	public static string ToStringDefault(this uint value) => value.ToString(Integer);
	public static string ToStringDefault(this ulong value) => value.ToString(Integer);

	public static string ToStringDefault(this TimeSpan value) => value.ToString(@"hh\:mm\:ss\.ff");
	public static string ToStringDefault(this DateTime value) => value.ToString("HH:mm:ss");

	public static string ToStringPercentage(this float value) => value.ToString("P1");
	public static string ToStringPercentage(this double value) => value.ToString("P2");

	public static string ToStringData(this uint value) => value.ToStringMetric() + 'B';
	public static string ToStringData(this ulong value) => value.ToStringMetric() + 'B';

	public static string ToStringDefault(this in Guid guid)
	{
		Span<char> span = stackalloc char[80]; //36x2 + a bit extra
		bool formatted = guid.TryFormat(span, out int length, "D");
		Assert.IsTrue(formatted && length <= span.Length / 2);

		//Convert to upper case
		ReadOnlySpan<char> lower = span[..length];
		Span<char> upper = span[length..(length + length)];
		lower.ToUpperInvariant(upper);
		return new string(upper);
	}

	public static string ToStringShort(this in Guid guid) => $"0x{guid.GetHashCode():X8}";

	/// <summary>
	/// Formats <paramref name="value"/> to its abbreviations using metric prefixes.
	/// Adapted from <see cref="Scalars"/> in <see cref="CodeHelpers.Mathematics"/>.
	/// </summary>
	/// <returns>The formatted <see cref="string"/> (eg. 123.4K, 12.34M, or 1.234G).</returns>
	/// <remarks>The returned <see cref="string"/> will also have a <see cref="string.Length"/> of 6 or shorter.</remarks>
	public static string ToStringMetric(this uint value, IFormatProvider provider = null)
	{
		const uint L3 = (uint)1E9;
		const uint L2 = (uint)1E6;
		const uint L1 = (uint)1E3;

		var info = NumberFormatInfo.GetInstance(provider);
		char separator = info.NumberDecimalSeparator[0];

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

			return MergeMetricParts(head, tail, separator, suffix);
		}
	}

	/// <inheritdoc cref="ToStringMetric(uint, System.IFormatProvider)"/>
	public static string ToStringMetric(this ulong value, IFormatProvider provider = null)
	{
		const ulong L6 = (ulong)1E18;
		const ulong L5 = (ulong)1E15;
		const ulong L4 = (ulong)1E12;
		const ulong L3 = (ulong)1E9;
		const ulong L2 = (ulong)1E6;
		const ulong L1 = (ulong)1E3;

		var info = NumberFormatInfo.GetInstance(provider);
		char separator = info.NumberDecimalSeparator[0];

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

			return MergeMetricParts((uint)head, (uint)tail, separator, suffix);
		}
	}

	static string MergeMetricParts(uint head, uint tail, char separator, char suffix)
	{
		Assert.IsTrue(head < 1000);
		Assert.IsTrue(tail < 1000);

		//Create buffer, 123.456 is the maximum length
		Span<char> span = stackalloc char[7];

		//Format into span
		bool formatted = head.TryFormat(span, /*            */ out int length0)
					   & tail.TryFormat(span[(length0 + 1)..], out int length1, "D3");

		Assert.IsTrue(formatted);

		//Add the separator and the suffix
		int length = Math.Min(length0 + 1 + length1 + 1, 6);

		span[length0] = separator;
		span[length - 1] = suffix;

		return new string(span[..length]);
	}
}