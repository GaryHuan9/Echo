using System;
using CharSpan = System.ReadOnlySpan<char>;

namespace Echo.Core.Common.Packed;
#if NET6_0
static class Formatting
{
	public static bool Begin(Span<char> destination, out int charsWritten)
	{
		if (destination.IsEmpty)
		{
			charsWritten = default;
			return false;
		}

		destination[0] = '(';
		charsWritten = 1;
		return true;
	}

	public static bool AppendOne<T>(Span<char> destination, T item, ref int charsWritten, CharSpan format, IFormatProvider provider) where T : ISpanFormattable
	{
		if (!Append(destination, item, ref charsWritten, format, provider)) return false;
		if (destination.Length < charsWritten + 2) return false;

		charsWritten += 2;
		destination[charsWritten - 2] = ',';
		destination[charsWritten - 1] = ' ';
		return true;
	}

	public static bool AppendEnd<T>(Span<char> destination, T item, ref int charsWritten, CharSpan format, IFormatProvider provider) where T : ISpanFormattable
	{
		if (!Append(destination, item, ref charsWritten, format, provider)) return false;
		if (destination.Length < charsWritten + 1) return false;

		destination[charsWritten] = ')';
		++charsWritten;
		return true;
	}

	static bool Append<T>(Span<char> destination, T item, ref int charsWritten, CharSpan format, IFormatProvider provider) where T : ISpanFormattable
	{
		if (!item.TryFormat(destination[charsWritten..], out int written, format, provider)) return false;

		charsWritten += written;
		return true;
	}
}
#endif

partial struct Float2
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, Y, ref charsWritten, format, provider);
#endif
}

partial struct Float3
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)}, {Z.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Y, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, Z, ref charsWritten, format, provider);
#endif
}

partial struct Float4
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)}, {Z.ToString(format, provider)}, {W.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Y, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Z, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, W, ref charsWritten, format, provider);
#endif
}

partial struct Int2
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, Y, ref charsWritten, format, provider);
#endif
}

partial struct Int3
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)}, {Z.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Y, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, Z, ref charsWritten, format, provider);
#endif
}

partial struct Int4
{
	public override string ToString() => ToString(default);
	public string ToString(string format, IFormatProvider provider = null) => $"({X.ToString(format, provider)}, {Y.ToString(format, provider)}, {Z.ToString(format, provider)}, {W.ToString(format, provider)})";

#if NET6_0
	public bool TryFormat(Span<char> destination, out int charsWritten, CharSpan format = default, IFormatProvider provider = null) =>
		Formatting.Begin(destination, out charsWritten) &&
		Formatting.AppendOne(destination, X, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Y, ref charsWritten, format, provider) &&
		Formatting.AppendOne(destination, Z, ref charsWritten, format, provider) &&
		Formatting.AppendEnd(destination, W, ref charsWritten, format, provider);
#endif
}