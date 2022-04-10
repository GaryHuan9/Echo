using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace EchoRenderer.Common.Coloring;

/// <summary>
/// A color type with red, green, blue, and alpha channels, occupying 16 bytes or 128 bits.
/// The value for these four channels are always larger than or equals to zero, or less than
/// <see cref="float.PositiveInfinity"/>, and is never <see cref="float.NaN"/>.
/// </summary>
public readonly struct RGBA128 : IColor<RGBA128>, IFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA128(float r, float g, float b, float alpha = 1f) : this(CheckInput(new Float4(r, g, b, alpha))) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA128(float value, float alpha = 1f) : this(CheckInput(new Float4(value, value, value, alpha))) { } //OPTIMIZE use shuffle?

	/// <summary>
	/// Create an <see cref="RGBA128"/> without any checks.
	/// </summary>
	RGBA128(in Float4 value) => d = value;

	readonly Float4 d;

	/// <summary>
	/// Returns the value of the alpha/transparency channel of this <see cref="RGBA128"/>.
	/// </summary>
	public float Alpha => d.W;

	/// <summary>
	/// Returns this <see cref="RGBA128"/> with the alpha channel assigned to one.
	/// </summary>
	public RGBA128 AlphaOne
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(new Float4(d.X, d.Y, d.Z, 1f)); //OPTIMIZE use FMA
	}

	public static RGBA128 Zero => new(Float4.Zero);
	public static RGBA128 Black => new(Float4.Ana);
	public static RGBA128 White => new(Float4.One);

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(string.Empty);

	public RGBA128 ToRGBA128() => this;
	public RGBA128 FromRGBA128(in RGBA128 value) => value;

	public string ToString(string format) => ToString(format, CultureInfo.InvariantCulture);
	public string ToString(string format, IFormatProvider provider) => d.ToString(format, provider);

	/// <summary>
	/// Returns this <see cref="RGBA128"/> converted as <typeparamref name="T"/> using <see cref="IColor{T}.FromRGBA128"/>.
	/// </summary>
	public T As<T>() where T : IColor<T> => default(T)!.FromRGBA128(this);

	public static RGBA128 Parse(ReadOnlySpan<char> span)
	{
		if (TryParse(span, out RGBA128 result)) return result;
		throw new Exception($"Cannot parse {span.ToString()}!");
	}

	public static bool TryParse(ReadOnlySpan<char> span, out RGBA128 result)
	{
		throw new NotImplementedException();

		// span = span.Trim();
		// span = span.Trim('#');
		// span = span.Trim("0x");
		//
		// result = span.Length switch
		// {
		// 	3 => (RGBA32)(), new Color32(ParseOne(span[0]), ParseOne(span[1]), ParseOne(span[2])),
		// 4 => (Float4)new Color32(ParseOne(span[0]), ParseOne(span[1]), ParseOne(span[2]), ParseOne(span[3])),
		// 6 => (Float4)new Color32(ParseTwo(span[..2]), ParseTwo(span[2..4]), ParseTwo(span[4..])),
		// 8 => (Float4)new Color32(ParseTwo(span[..2]), ParseTwo(span[2..4]), ParseTwo(span[4..6]), ParseTwo(span[6..]))
		// };
		//
		// static float ParseOne(char span)
		// {
		// 	int hex = ParseHex(span);
		// 	return hex * 16 + hex;
		// }
		//
		// static byte ParseTwo(ReadOnlySpan<char> span) => (byte)(ParseHex(span[0]) * 16 + ParseHex(span[1]));
		//
		// static int ParseHex(char span) => span switch
		// {
		// 	>= '0' and <= '9' => span - '0',
		// 	>= 'A' and <= 'F' => span - 'A' + 10,
		// 	>= 'a' and <= 'f' => span - 'a' + 10,
		// 	_ => throw ExceptionHelper.Invalid(nameof(span), span, "is not valid hex")
		// };
	}

	public static implicit operator Float4(in RGBA128 value) => value.d;
	public static explicit operator RGBA128(in Float4 value) => new(CheckInput(value));
	public static explicit operator RGBA128(in RGB128 value) => new RGBA128(value).AlphaOne;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Float4 CheckInput(in Float4 value)
	{
		Assert.IsTrue(value >= Float4.Zero);
		Assert.IsTrue(float.IsFinite(value.Sum));

		return value;
	}
}