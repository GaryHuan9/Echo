using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace EchoRenderer.Common.Mathematics.Primitives;

public readonly struct RGBA32 : IFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA32(float value, float alpha = 1f) : this(new Float4(value, value, value, alpha))
	{
		Assert.IsTrue(value >= 0f);
		Assert.IsTrue(alpha >= 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA32(float r, float g, float b, float alpha = 1f) : this(new Float4(r, g, b, alpha))
	{
		Assert.IsTrue(r >= 0f);
		Assert.IsTrue(g >= 0f);
		Assert.IsTrue(b >= 0f);
		Assert.IsTrue(alpha >= 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGBA32(in Float3 value, float alpha = 1f) : this(new Float4(value.X, value.Y, value.Z, alpha))
	{
		Assert.IsTrue(value >= Float3.Zero);
		Assert.IsTrue(alpha >= 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	RGBA32(in Float4 value)
	{
		Assert.IsFalse(float.IsNaN(value.Product));
		Assert.IsTrue(float.IsFinite(value.Product));
		d = value;
	}

	readonly Float4 d;

	const float EpsilonWeight = 1E-5f;
	const float RadianceWeightR = 0.2126f;
	const float RadianceWeightG = 0.7152f;
	const float RadianceWeightB = 0.0722f;

	public float Luminance => d.Dot(RadianceWeights);

	public bool IsZero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => d <= new Float4
		(
			EpsilonWeight / RadianceWeightR,
			EpsilonWeight / RadianceWeightG,
			EpsilonWeight / RadianceWeightB,
			float.MaxValue
		);
	}

	/// <summary>
	/// Returns this <see cref="RGBA32"/> with the alpha channel assigned to one.
	/// </summary>
	public RGBA32 AlphaOne
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(new Float4(d.X, d.Y, d.Z, 1f));
	}

	public static RGBA32 Zero => default;
	public static RGBA32 Black => new(Float4.Ana);
	public static RGBA32 White => new(Float4.One);

	static Float4 RadianceWeights => new(RadianceWeightR, RadianceWeightG, RadianceWeightB, 0f);

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(string.Empty);

	public string ToString(string format) => ToString(format, CultureInfo.InvariantCulture);
	public string ToString(string format, IFormatProvider provider) => d.ToString(format, provider);

	public static RGBA32 Parse(ReadOnlySpan<char> span)
	{
		if (TryParse(span, out RGBA32 result)) return result;
		throw new Exception($"Cannot parse {span.ToString()}!");
	}

	public static bool TryParse(ReadOnlySpan<char> span, out RGBA32 result)
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

	public static RGBA32 operator +(in RGBA32 first, in RGBA32 second) => new(first.d + second.d);
	public static RGBA32 operator -(in RGBA32 first, in RGBA32 second) => (RGBA32)(first.d - second.d);

	public static RGBA32 operator *(in RGBA32 first, in RGBA32 second) => new(first.d * second.d);
	public static RGBA32 operator /(in RGBA32 first, in RGBA32 second) => new(first.d / second.d);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RGBA32 operator *(in RGBA32 first, in float second)
	{
		Assert.IsTrue(second >= 0f);
		return new RGBA32(first.d * second);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RGBA32 operator /(in RGBA32 first, in float second)
	{
		Assert.IsTrue(FastMath.Positive(second));
		return new RGBA32(first.d / second);
	}

	public static RGBA32 operator *(in float first, in RGBA32 second)
	{
		Assert.IsTrue(first >= 0f);
		return new RGBA32(first * second.d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RGBA32 operator /(in float first, in RGBA32 second)
	{
		Assert.IsTrue(FastMath.Positive(first));
		return new RGBA32(first / second.d);
	}

	public static implicit operator Float4(in RGBA32 value) => value.d;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator RGBA32(in Float4 value)
	{
		Assert.IsTrue(value >= Float4.Zero);
		return new RGBA32(value);
	}
}