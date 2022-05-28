using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Core.Textures.Colors;

/// <summary>
/// A color type with red, green, blue, and alpha channels, occupying 16 bytes or 128 bits.
/// The value for these four channels are always larger than or equals to zero, or less than
/// <see cref="float.PositiveInfinity"/>, and is never <see cref="float.NaN"/>.
/// </summary>
public readonly partial struct RGBA128 : IColor<RGBA128>, IFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA128(float r, float g, float b, float alpha = 1f) : this(CheckInput(new Float4(r, g, b, alpha))) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA128(float value, float alpha = 1f) : this(CheckInput(new Float4(value, value, value, alpha))) { } //OPTIMIZE use shuffle?

	/// <summary>
	/// Create an <see cref="RGBA128"/> without any checks.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		//Replaces the W component of the Float4 with 1; see Float4.XYZ_ for more info
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(new Float4(Sse41.Blend(d.v, Vector128.Create(1f), 0b1000)));
	}

	public static RGBA128 Zero => new(Float4.Zero);
	public static RGBA128 Black => new(Float4.Ana);
	public static RGBA128 White => new(Float4.One);

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(default);

	/// <inheritdoc/>
	public RGBA128 ToRGBA128() => this;

	/// <inheritdoc/>
	public RGBA128 FromRGBA128(in RGBA128 value) => value;

	/// <inheritdoc cref="ToString()"/>
	public string ToString(string format, IFormatProvider provider = null) => d.ToString(format, provider);

	/// <summary>
	/// Returns this <see cref="RGBA128"/> converted as <typeparamref name="T"/> using <see cref="IColor{T}.FromRGBA128"/>.
	/// </summary>
	public T As<T>() where T : unmanaged, IColor<T> => default(T)!.FromRGBA128(this);

	/// <summary>
	/// Parses the input <paramref name="span"/> as a <typeparamref name="T"/> and returns the
	/// result. An exception is thrown if the <paramref name="span"/> format is invalid.
	/// </summary>
	public static T Parse<T>(ReadOnlySpan<char> span) where T : unmanaged, IColor<T> => Parse(span).As<T>();

	/// <summary>
	/// Parses the input <paramref name="span"/> as a <see cref="RGBA128"/> and returns the
	/// result. An exception is thrown if the <paramref name="span"/> format is invalid.
	/// </summary>
	public static RGBA128 Parse(ReadOnlySpan<char> span)
	{
		if (TryParse(span, out RGBA128 result)) return result;
		throw new Exception($"Cannot parse {span.ToString()}!");
	}

	/// <summary>
	/// Tries to parse the input <paramref name="span"/> as a <typeparamref name="T"/> and outputs
	/// to <paramref name="result"/>. Returns true if the operation was successful, false otherwise.
	/// </summary>
	public static bool TryParse<T>(ReadOnlySpan<char> span, out T result) where T : unmanaged, IColor<T>
	{
		bool successful = TryParse(span, out RGBA128 parsed);

		result = parsed.As<T>();
		return successful;
	}

	/// <summary>
	/// Tries to parse the input <paramref name="span"/> as a <see cref="RGBA128"/> and outputs
	/// to <paramref name="result"/>. Returns true if the operation was successful, false otherwise.
	/// </summary>
	public static bool TryParse(ReadOnlySpan<char> span, out RGBA128 result) => new Parser(span).Execute(out result);

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